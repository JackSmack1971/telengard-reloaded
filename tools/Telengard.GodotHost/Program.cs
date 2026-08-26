using System.Text.Json;
using System.Net;
using System.Text;
using Telengard.Content;
using Telengard.Core.Presentation;
using Telengard.Core.Simulation;
using Telengard.Core.Events;
using Telengard.Core.Combat;
using Telengard.Core.Items;
using Telengard.Core.Magic;
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
                return RunServer(contentRoot, ReadPort(args), args);
            }

            var session = CreateSession(contentRoot, ReadGameplayConfiguration(args));
            WriteJson(session.Frame());
            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or KeyNotFoundException or FormatException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunServer(string contentRoot, int port, string[] args)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        var session = CreateSession(contentRoot, ReadGameplayConfiguration(args));
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
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or JsonException or OverflowException or KeyNotFoundException or FormatException)
            {
                WriteResponse(context.Response, new { accepted = false, error = exception.Message, frame = session.Frame().frame });
            }
        }
    }

    private static GodotSession CreateSession(string contentRoot, GodotGameplayConfiguration? gameplayConfiguration)
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
        return new GodotSession(
            dispatcher,
            new FloorLayoutCache(120, GameVersions.Current.GeneratorVersion),
            committedEvents,
            pack,
            gameplayConfiguration);
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

    private static GodotGameplayConfiguration? ReadGameplayConfiguration(string[] args)
    {
        var index = Array.IndexOf(args, "--gameplay-config");
        if (index < 0) return null;
        if (index + 1 >= args.Length) throw new ArgumentException("Usage: --gameplay-config <path>.");

        var path = Path.GetFullPath(args[index + 1]);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        return new GodotGameplayConfiguration(
            new AttackConfiguration(root.GetProperty("attack_damage").GetInt32()),
            new FleeConfiguration(root.GetProperty("flee_success_chance").GetDouble()),
            new ThreatClassificationConfiguration(
                root.GetProperty("trivial_maximum_level_difference").GetInt32(),
                root.GetProperty("deadly_minimum_level_difference").GetInt32(),
                root.GetProperty("known_monster_definition_ids").EnumerateArray().Select(value => value.GetString()!)));
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

public sealed class GodotSession
{
    private readonly CommandDispatcher _dispatcher;
    private readonly FloorLayoutCache _layouts;
    private readonly List<IDomainEvent> _events;
    private readonly ContentPack _contentPack;
    private readonly GodotGameplayConfiguration? _gameplayConfiguration;
    private readonly SimulationClock _clock = new(10, 0.2);

    public GodotSession(
        CommandDispatcher dispatcher,
        FloorLayoutCache layouts,
        List<IDomainEvent> events,
        ContentPack contentPack,
        GodotGameplayConfiguration? gameplayConfiguration = null)
    {
        _dispatcher = dispatcher;
        _layouts = layouts ?? throw new ArgumentNullException(nameof(layouts));
        _events = events;
        _contentPack = contentPack;
        _gameplayConfiguration = gameplayConfiguration;
        _dispatcher.Register<AdvanceSimulationCommand>(SimulationTimeResolver.Advance);
        _dispatcher.Register<EnterDungeonCommand>((state, command) => DungeonWalkingResolver.Enter(state, command, _layouts.Get(1)));
        _dispatcher.Register<MoveCommand>((state, command) => DungeonWalkingResolver.Move(state, command, _layouts.Get(state.Player.Position.Floor)));
        _dispatcher.Register<ChangeFloorCommand>((state, command) =>
            FloorTransitionResolver.Apply(
                state,
                command,
                _layouts.Get(state.Player.Position.Floor),
                _layouts.Get(ValidateTargetFloor(state, command))));
        _dispatcher.Register<LeaveDungeonCommand>((state, command) => DungeonWalkingResolver.Leave(state, command, _layouts.Get(1)));
        _dispatcher.Register<SelectCombatActionCommand>(CombatStateResolver.SelectAction);
        _dispatcher.Register<AdvanceCombatCommand>(CombatStateResolver.Advance);
        _dispatcher.Register<AssessThreatCommand>(ResolveThreat);
        _dispatcher.Register<DefendCommand>(DefendResolver.Resolve);
        _dispatcher.Register<CastSpellCommand>((state, command) =>
            SpellCastResolver.Resolve(state, command, _contentPack.Spells.GetRequired(command.SpellId)));
        _dispatcher.Register<EquipItemCommand>(EquipmentResolver.Equip);
        _dispatcher.Register<UnequipItemCommand>(EquipmentResolver.Unequip);
        if (_gameplayConfiguration is not null)
        {
            _dispatcher.Register<AttackCommand>((state, command) =>
                AttackResolver.Resolve(state, command, _gameplayConfiguration.Attack));
            _dispatcher.Register<FleeCommand>((state, command) =>
                FleeResolver.Resolve(state, command, _gameplayConfiguration.Flee));
        }
        _dispatcher.Register<ActivateFeatureCommand>(FeatureActivationResolver.Activate);
    }

    public GameState CurrentState => _dispatcher.CurrentState;

    public ContentPack ContentPack => _contentPack;

    public object Dispatch(JsonElement request)
    {
        var type = request.GetProperty("type").GetString();
        switch (type)
        {
            case "enter_dungeon": _dispatcher.Dispatch(new EnterDungeonCommand()); break;
            case "move": _dispatcher.Dispatch(new MoveCommand(Enum.Parse<MovementDirection>(request.GetProperty("direction").GetString()!, true))); break;
            case "change_floor": _dispatcher.Dispatch(new ChangeFloorCommand(Enum.Parse<StairDirection>(request.GetProperty("direction").GetString()!, true))); break;
            case "leave_dungeon": _dispatcher.Dispatch(new LeaveDungeonCommand()); break;
            case "combat_action": _dispatcher.Dispatch(new SelectCombatActionCommand(Enum.Parse<CombatAction>(request.GetProperty("action").GetString()!, true))); break;
            case "advance_combat": _dispatcher.Dispatch(new AdvanceCombatCommand()); break;
            case "assess_threat": _dispatcher.Dispatch(new AssessThreatCommand()); break;
            case "resolve_combat_action": ResolveCombatAction(request); break;
            case "cast_spell": _dispatcher.Dispatch(new CastSpellCommand(request.GetProperty("spell_id").GetString()!)); break;
            case "equip_item":
                _dispatcher.Dispatch(new EquipItemCommand(
                    request.GetProperty("slot_id").GetString()!,
                    Guid.Parse(request.GetProperty("item_instance_id").GetString()!)));
                break;
            case "unequip_item": _dispatcher.Dispatch(new UnequipItemCommand(request.GetProperty("slot_id").GetString()!)); break;
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

    private static int TargetFloor(GameState state, ChangeFloorCommand command) =>
        state.Player.Position.Floor + (command.Direction is StairDirection.Down ? 1 : -1);

    private static int ValidateTargetFloor(GameState state, ChangeFloorCommand command)
    {
        var targetFloor = TargetFloor(state, command);
        if (targetFloor is < 1 or > 5)
            throw new InvalidOperationException("The hosted MVP session supports floors 1 through 5.");
        return targetFloor;
    }

    private CommandResult ResolveThreat(GameState state, AssessThreatCommand command)
    {
        return ThreatAssessmentResolver.Resolve(
            state,
            command,
            _gameplayConfiguration?.Threat
                ?? throw new InvalidOperationException("Combat gameplay configuration is required for threat assessment."));
    }

    private void ResolveCombatAction(JsonElement request)
    {
        var action = _dispatcher.CurrentState.Combat?.SelectedAction
            ?? throw new InvalidOperationException("No combat action is selected.");
        switch (action)
        {
            case CombatAction.Attack: _dispatcher.Dispatch(new AttackCommand()); break;
            case CombatAction.Defend: _dispatcher.Dispatch(new DefendCommand()); break;
            case CombatAction.Flee: _dispatcher.Dispatch(new FleeCommand()); break;
            case CombatAction.CastSpell:
                _dispatcher.Dispatch(new CastSpellCommand(request.GetProperty("spell_id").GetString()!));
                break;
            default: throw new InvalidOperationException($"Combat action '{action}' has no Core resolver.");
        }
    }

    public SessionFrame Frame()
    {
        var projection = PresentationStateAdapter.Create(_dispatcher.CurrentState);
        var frame = ModernRenderer.Create(projection, _events);
        return new SessionFrame(Program.FrameJson(frame), projection.SimulationTick);
    }
}

public sealed record GodotGameplayConfiguration
{
    public GodotGameplayConfiguration(
        AttackConfiguration attack,
        FleeConfiguration flee,
        ThreatClassificationConfiguration threat)
    {
        Attack = attack ?? throw new ArgumentNullException(nameof(attack));
        Flee = flee ?? throw new ArgumentNullException(nameof(flee));
        Threat = threat ?? throw new ArgumentNullException(nameof(threat));
    }

    public AttackConfiguration Attack { get; }
    public FleeConfiguration Flee { get; }
    public ThreatClassificationConfiguration Threat { get; }
}

public sealed record SessionFrame(object frame, long tick);
