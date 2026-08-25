using System.Text.Json;
using System.Net;
using System.Text;
using Telengard.Content;
using Telengard.Core.Presentation;
using Telengard.Core.Simulation;
using Telengard.Core.Events;
using Telengard.Core.Combat;
using Telengard.Core.World.Generation;
using Telengard.Core.World.Features;

namespace Telengard.GodotHost;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var contentRoot = ReadContentRoot(args);
            if (args.Contains("--serve", StringComparer.Ordinal))
            {
                return RunServer(contentRoot, ReadPort(args));
            }

            var session = CreateSession(contentRoot);
            WriteJson(session.Frame());
            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunServer(string contentRoot, int port)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        var session = CreateSession(contentRoot);
        while (true)
        {
            var context = listener.GetContext();
            try
            {
                var response = context.Request.HttpMethod == "POST" && context.Request.Url?.AbsolutePath == "/command"
                    ? session.Dispatch(JsonDocument.Parse(new StreamReader(context.Request.InputStream).ReadToEnd()).RootElement)
                    : session.Frame();
                WriteResponse(context.Response, response);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or JsonException or OverflowException)
            {
                WriteResponse(context.Response, new { accepted = false, error = exception.Message, frame = session.Frame().frame });
            }
        }
    }

    private static GodotSession CreateSession(string contentRoot)
    {
        var pack = ContentPackLoader.Load(contentRoot);
        var playerId = Guid.Parse("00000000-0000-0000-0000-000000000120");
        var initial = GameState.Create(120, playerId: playerId);
        var provider = new RolledCharacterCreationProvider(
            new RolledCharacterCreationConfiguration(
                "godot-bootstrap-v1",
                Enumerable.Repeat(new RolledAttributeRange(3, 18), 6)));
        var character = provider.Create(initial, new CharacterCreationRequest(CharacterCreationMode.Rolled));
        var result = NewGameSetupResolver.Create(new NewGameSetupRequest(120, GameMode.Classic, character));
        var eventBus = new DomainEventBus();
        var committedEvents = new List<IDomainEvent>();
        eventBus.Subscribe<IDomainEvent>(committedEvents.Add);
        var dispatcher = new CommandDispatcher(result.State, eventBus);
        eventBus.Publish(result.Events);
        return new GodotSession(dispatcher, new FloorLayoutGenerator().Generate(120, GameVersions.Current.GeneratorVersion, 1), committedEvents);
    }

    private static int ReadPort(string[] args)
    {
        var index = Array.IndexOf(args, "--port");
        return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out var port) ? port : 18120;
    }

    private static void WriteJson(object value) => Console.WriteLine(JsonSerializer.Serialize(value));

    private static void WriteResponse(HttpListenerResponse response, object value)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        response.ContentType = "application/json";
        response.ContentLength64 = bytes.Length;
        response.OutputStream.Write(bytes);
        response.Close();
    }

    private static string ReadContentRoot(string[] args)
    {
        var index = Array.IndexOf(args, "--content-root");
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Usage: --content-root <path>.");
        return Path.GetFullPath(args[index + 1]);
    }

    internal static object FrameJson(ModernRenderFrame frame) => new
    {
        scene = frame.Scene.ToString().ToLowerInvariant(),
        player_position = Position(frame.PlayerPosition),
        environment = new { dynamic_lighting = frame.Environment.DynamicLighting, atmospheric_effects = frame.Environment.AtmosphericEffects, theme_id = frame.Environment.ThemeId },
        tiles = frame.Tiles.Select(tile => new { position = Position(tile.Position), knowledge = tile.Knowledge.ToString().ToLowerInvariant(), connections = tile.Connections.ToString() }),
        features = frame.Features.Select(feature => new { instance_id = feature.InstanceId, definition_id = feature.DefinitionId, presentation_key = feature.PresentationKey, position = Position(feature.Position), activation_count = feature.ActivationCount }),
        combat = frame.Combat is null ? null : new
        {
            encounter_id = frame.Combat.EncounterId,
            phase = frame.Combat.Phase.ToString().ToLowerInvariant(),
            round = frame.Combat.Round,
            threat_level = frame.Combat.ThreatLevel?.ToString().ToLowerInvariant(),
            monster = new
            {
                instance_id = frame.Combat.Monster.InstanceId,
                definition_id = frame.Combat.Monster.DefinitionId,
                presentation_key = frame.Combat.Monster.PresentationKey,
                current_hit_points = frame.Combat.Monster.CurrentHitPoints,
                position = Position(frame.Combat.Monster.Position)
            }
        },
        hud = new { player_id = frame.Hud.PlayerId, level = frame.Hud.Level, hit_points = frame.Hud.HitPoints, max_hit_points = frame.Hud.MaxHitPoints, spell_power = frame.Hud.SpellPower, max_spell_power = frame.Hud.MaxSpellPower, carried_gold = frame.Hud.CarriedGold, secured_gold = frame.Hud.SecuredGold, alive = frame.Hud.Alive },
        inventory = frame.Inventory,
        spells = frame.Spells,
        journal = frame.Journal
    };

    private static object Position(DungeonPosition position) => new { floor = position.Floor, x = position.X, y = position.Y };
}

internal sealed class GodotSession
{
    private readonly CommandDispatcher _dispatcher;
    private readonly FloorLayout _layout;
    private readonly List<IDomainEvent> _events;
    private readonly SimulationClock _clock = new(10, 0.2);

    public GodotSession(CommandDispatcher dispatcher, FloorLayout layout, List<IDomainEvent> events)
    {
        _dispatcher = dispatcher;
        _layout = layout;
        _events = events;
        _dispatcher.Register<AdvanceSimulationCommand>(SimulationTimeResolver.Advance);
        _dispatcher.Register<EnterDungeonCommand>((state, command) => DungeonWalkingResolver.Enter(state, command, _layout));
        _dispatcher.Register<MoveCommand>((state, command) => DungeonWalkingResolver.Move(state, command, _layout));
        _dispatcher.Register<SelectCombatActionCommand>(CombatStateResolver.SelectAction);
        _dispatcher.Register<ActivateFeatureCommand>(FeatureActivationResolver.Activate);
    }

    public object Dispatch(JsonElement request)
    {
        var type = request.GetProperty("type").GetString();
        switch (type)
        {
            case "enter_dungeon": _dispatcher.Dispatch(new EnterDungeonCommand()); break;
            case "move": _dispatcher.Dispatch(new MoveCommand(Enum.Parse<MovementDirection>(request.GetProperty("direction").GetString()!, true))); break;
            case "combat_action": _dispatcher.Dispatch(new SelectCombatActionCommand(Enum.Parse<CombatAction>(request.GetProperty("action").GetString()!, true))); break;
            case "interact":
                var position = _dispatcher.CurrentState.Player.Position;
                var feature = _dispatcher.CurrentState.Dungeon.Features.SingleOrDefault(candidate => candidate.Discovered && candidate.Position == position)
                    ?? throw new InvalidOperationException("There is no discovered feature at the current position.");
                _dispatcher.Dispatch(new ActivateFeatureCommand(feature.InstanceId));
                break;
            case "time_mode": _clock.SetMode(Enum.Parse<SimulationTimeMode>(request.GetProperty("mode").GetString()!, true)); break;
            case "advance":
                var ticks = _clock.Advance(request.GetProperty("elapsed_seconds").GetDouble());
                if (ticks > 0) _dispatcher.Dispatch(new AdvanceSimulationCommand(ticks));
                break;
            default: throw new ArgumentException($"Unknown client intent '{type}'.");
        }
        return new { accepted = true, frame = Frame().frame };
    }

    public SessionFrame Frame()
    {
        var projection = PresentationStateAdapter.Create(_dispatcher.CurrentState);
        var frame = ModernRenderer.Create(projection, _events);
        return new SessionFrame(Program.FrameJson(frame), projection.SimulationTick);
    }
}

internal sealed record SessionFrame(object frame, long tick);
