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
using System.Security.Cryptography;

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
        var layout = new FloorLayoutGenerator().Generate(120, GameVersions.Current.GeneratorVersion, 1);
        var configuredState = gameplayConfiguration?.ApplyBootstrap(result.State, pack, layout) ?? result.State;
        var eventBus = new DomainEventBus();
        var committedEvents = new List<IDomainEvent>();
        eventBus.Subscribe<IDomainEvent>(committedEvents.Add);
        var dispatcher = new CommandDispatcher(configuredState, eventBus);
        eventBus.Publish(result.Events);
        return new GodotSession(
            dispatcher,
            layout,
            committedEvents,
            pack,
            gameplayConfiguration,
            gameplayConfiguration?.CreateEncounterConfiguration(pack),
            gameplayConfiguration?.CreateTeleporterDestinations(layout, result.State.WorldSeed));
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
                root.GetProperty("known_monster_definition_ids").EnumerateArray().Select(value => value.GetString()!)),
            GodotBootstrapConfiguration.FromJson(root));
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
                position = Position(frame.Combat.Monster.Position)
            }
        },
        hud = new { player_id = frame.Hud.PlayerId, level = frame.Hud.Level, hit_points = frame.Hud.HitPoints, max_hit_points = frame.Hud.MaxHitPoints, spell_power = frame.Hud.SpellPower, max_spell_power = frame.Hud.MaxSpellPower, carried_gold = frame.Hud.CarriedGold, secured_gold = frame.Hud.SecuredGold, alive = frame.Hud.Alive },
        inventory = frame.Inventory,
        spells = frame.Spells,
        journal = frame.Journal,
        equipment = frame.Equipment.Select(slot => new { slot_id = slot.SlotId, equipped = slot.Equipped, item_instance_id = slot.ItemInstanceId }),
        cues = frame.Cues.Select(cue => new { kind = cue.Kind.ToString(), value = cue.Value, details = cue.Details, entity_id = cue.EntityId })
    };

    private static object Position(DungeonPosition position) => new { floor = position.Floor, x = position.X, y = position.Y };
}

public sealed class GodotSession
{
    private readonly CommandDispatcher _dispatcher;
    private readonly FloorLayout _layout;
    private readonly List<IDomainEvent> _events;
    private readonly ContentPack _contentPack;
    private readonly GodotGameplayConfiguration? _gameplayConfiguration;
    private readonly EncounterTriggerConfiguration? _encounterConfiguration;
    private readonly IReadOnlyDictionary<Guid, DungeonPosition> _teleporterDestinations;
    private readonly SimulationClock _clock = new(10, 0.2);

    public GodotSession(
        CommandDispatcher dispatcher,
        FloorLayout layout,
        List<IDomainEvent> events,
        ContentPack contentPack,
        GodotGameplayConfiguration? gameplayConfiguration = null,
        EncounterTriggerConfiguration? encounterConfiguration = null,
        IReadOnlyDictionary<Guid, DungeonPosition>? teleporterDestinations = null)
    {
        _dispatcher = dispatcher;
        _layout = layout;
        _events = events;
        _contentPack = contentPack;
        _gameplayConfiguration = gameplayConfiguration;
        _encounterConfiguration = encounterConfiguration;
        _teleporterDestinations = teleporterDestinations ?? new Dictionary<Guid, DungeonPosition>();
        _dispatcher.Register<AdvanceSimulationCommand>(SimulationTimeResolver.Advance);
        _dispatcher.Register<EnterDungeonCommand>((state, command) => DungeonWalkingResolver.Enter(state, command, _layout));
        _dispatcher.Register<MoveCommand>((state, command) => DungeonWalkingResolver.Move(state, command, _layout, _encounterConfiguration));
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
        _dispatcher.Register<ActivateFeatureCommand>(ResolveFeatureActivation);
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
                var feature = _dispatcher.CurrentState.Dungeon.Features.SingleOrDefault(candidate => candidate.Position == position)
                    ?? throw new InvalidOperationException("There is no feature at the current position.");
                _dispatcher.Dispatch(new ActivateFeatureCommand(feature.InstanceId));
                break;
            case "time_mode": _clock.SetMode(Enum.Parse<SimulationTimeMode>(request.GetProperty("mode").GetString()!, true)); break;
            case "advance":
                var ticks = _clock.Advance(request.GetProperty("elapsed_seconds").GetDouble());
                if (ticks > 0) _dispatcher.Dispatch(new AdvanceSimulationCommand(ticks));
                break;
            default: throw new ArgumentException($"Unknown client intent '{type}'.");
        }
        AdvanceCombatLifecycle();
        return new { accepted = true, frame = Frame().frame };
    }

    private void AdvanceCombatLifecycle()
    {
        while (_dispatcher.CurrentState.Combat?.Phase is
               CombatPhase.Searching or CombatPhase.Contact or CombatPhase.ThreatAssessment or
               CombatPhase.EnemyAction or CombatPhase.StateCheck)
        {
            if (_dispatcher.CurrentState.Combat.Phase == CombatPhase.ThreatAssessment)
            {
                _dispatcher.Dispatch(new AssessThreatCommand());
            }
            else
            {
                _dispatcher.Dispatch(new AdvanceCombatCommand());
            }
        }
    }

    private CommandResult ResolveThreat(GameState state, AssessThreatCommand command)
    {
        return ThreatAssessmentResolver.Resolve(
            state,
            command,
            _gameplayConfiguration?.Threat
                ?? throw new InvalidOperationException("Combat gameplay configuration is required for threat assessment."));
    }

    private CommandResult ResolveFeatureActivation(GameState state, ActivateFeatureCommand command)
    {
        var feature = state.Dungeon.Features.SingleOrDefault(candidate => candidate.InstanceId == command.FeatureId)
            ?? throw new InvalidOperationException("The requested feature does not exist in the current dungeon state.");
        var definition = _contentPack.Features.GetRequired(feature.DefinitionId);
        var context = new FeatureOutcomeSelectionContext();
        return definition.Type switch
        {
            FeatureType.Fountain => FountainResolver.Resolve(state, command, definition, context),
            FeatureType.Altar => AltarResolver.Resolve(state, command, definition, context),
            FeatureType.Pit => PitResolver.Resolve(state, command, definition, context),
            FeatureType.Teleporter => TeleporterResolver.Resolve(
                state,
                command,
                definition,
                _teleporterDestinations.GetValueOrDefault(feature.InstanceId)
                    ?? throw new InvalidOperationException("The teleporter destination is not configured."),
                context),
            _ => FeatureActivationResolver.Activate(state, command)
        };
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
        ThreatClassificationConfiguration threat,
        GodotBootstrapConfiguration? bootstrap = null)
    {
        Attack = attack ?? throw new ArgumentNullException(nameof(attack));
        Flee = flee ?? throw new ArgumentNullException(nameof(flee));
        Threat = threat ?? throw new ArgumentNullException(nameof(threat));
        Bootstrap = bootstrap;
    }

    public AttackConfiguration Attack { get; }
    public FleeConfiguration Flee { get; }
    public ThreatClassificationConfiguration Threat { get; }
    public GodotBootstrapConfiguration? Bootstrap { get; }

    public EncounterTriggerConfiguration? CreateEncounterConfiguration(ContentPack pack) =>
        Bootstrap is null || Bootstrap.EncounterTriggerChance == 0
            ? null
            : pack.CreateEncounterTriggerConfiguration(1, Bootstrap.EncounterTriggerChance);

    public GameState ApplyBootstrap(GameState state, ContentPack pack, FloorLayout layout)
    {
        if (Bootstrap is null) return state;
        if (Bootstrap.InitialMaxHitPoints <= 0 || Bootstrap.InitialHitPoints <= 0 || Bootstrap.InitialHitPoints > Bootstrap.InitialMaxHitPoints)
            throw new InvalidOperationException("The bootstrap hit-point values must be positive and within the configured maximum.");
        if (Bootstrap.InitialMaxSpellPower < 0 || Bootstrap.InitialSpellPower < 0 || Bootstrap.InitialSpellPower > Bootstrap.InitialMaxSpellPower)
            throw new InvalidOperationException("The bootstrap spell-power values must be non-negative and within the configured maximum.");
        if (Bootstrap.EncounterTriggerChance is < 0 or > 1)
            throw new InvalidOperationException("The bootstrap encounter chance must be between zero and one.");
        foreach (var spell in Bootstrap.StartingSpells) _ = pack.Spells.GetRequired(spell);
        foreach (var item in Bootstrap.StartingInventory) _ = pack.Items.GetRequired(item);
        var player = state.Player with
        {
            HitPoints = Bootstrap.InitialHitPoints,
            MaxHitPoints = Bootstrap.InitialMaxHitPoints,
            SpellPower = Bootstrap.InitialSpellPower,
            MaxSpellPower = Bootstrap.InitialMaxSpellPower,
            Spells = Bootstrap.StartingSpells,
            Inventory = Bootstrap.StartingInventory,
            EquipmentSlots = Bootstrap.EquipmentSlots.Select(slot => new EquipmentSlotState(slot)).ToArray()
        };
        var features = Bootstrap.FeaturePlacements.Select((placement, index) =>
        {
            var position = RoomPosition(layout, placement.RoomIndex);
            return new FeatureInstance(
                StableFeatureId(state.WorldSeed, placement.DefinitionId, position, index),
                pack.Features.GetRequired(placement.DefinitionId).Id,
                position);
        }).ToArray();
        return state with { Player = player, Dungeon = new DungeonState { Features = features } };
    }

    public IReadOnlyDictionary<Guid, DungeonPosition> CreateTeleporterDestinations(FloorLayout layout, long worldSeed)
    {
        if (Bootstrap is null) return new Dictionary<Guid, DungeonPosition>();
        var destinations = new Dictionary<Guid, DungeonPosition>();
        foreach (var (placement, index) in Bootstrap.FeaturePlacements
                     .Select((placement, index) => (placement, index))
                     .Where(pair => pair.placement.DefinitionId == "network-teleporter" && pair.placement.DestinationRoomIndex.HasValue)
                     .Select(pair => (pair.placement, pair.index)))
        {
            var position = RoomPosition(layout, placement.RoomIndex);
            destinations[StableFeatureId(worldSeed, placement.DefinitionId, position, index)] = RoomPosition(layout, placement.DestinationRoomIndex!.Value);
        }
        return destinations;
    }

    private static DungeonPosition RoomPosition(FloorLayout layout, int roomIndex)
    {
        if (roomIndex < 0 || roomIndex >= layout.Rooms.Count)
            throw new ArgumentOutOfRangeException(nameof(roomIndex), "The configured room index is outside the generated layout.");
        var room = layout.Rooms[roomIndex];
        return new DungeonPosition(layout.Floor, room.X + room.Width / 2, room.Y + room.Height / 2);
    }

    private static Guid StableFeatureId(long seed, string definitionId, DungeonPosition position, int index)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"godot-feature:{seed}:{definitionId}:{position}:{index}"));
        return new Guid(bytes[..16]);
    }
}

public sealed record GodotBootstrapConfiguration
{
    public int InitialHitPoints { get; init; }
    public int InitialMaxHitPoints { get; init; }
    public int InitialSpellPower { get; init; }
    public int InitialMaxSpellPower { get; init; }
    public IReadOnlyList<string> StartingSpells { get; init; } = [];
    public IReadOnlyList<string> StartingInventory { get; init; } = [];
    public IReadOnlyList<string> EquipmentSlots { get; init; } = [];
    public double EncounterTriggerChance { get; init; }
    public IReadOnlyList<GodotFeaturePlacement> FeaturePlacements { get; init; } = [];
    public static GodotBootstrapConfiguration FromJson(JsonElement root) => new()
    {
        InitialHitPoints = root.GetProperty("initial_hit_points").GetInt32(),
        InitialMaxHitPoints = root.GetProperty("initial_max_hit_points").GetInt32(),
        InitialSpellPower = root.GetProperty("initial_spell_power").GetInt32(),
        InitialMaxSpellPower = root.GetProperty("initial_max_spell_power").GetInt32(),
        StartingSpells = root.GetProperty("starting_spells").EnumerateArray().Select(value => value.GetString()!).ToArray(),
        StartingInventory = root.GetProperty("starting_inventory").EnumerateArray().Select(value => value.GetString()!).ToArray(),
        EquipmentSlots = root.GetProperty("equipment_slots").EnumerateArray().Select(value => value.GetString()!).ToArray(),
        EncounterTriggerChance = root.GetProperty("encounter_trigger_chance").GetDouble(),
        FeaturePlacements = root.GetProperty("feature_placements").EnumerateArray().Select(value => new GodotFeaturePlacement(
            value.GetProperty("definition_id").GetString()!,
            value.GetProperty("room_index").GetInt32(),
            value.TryGetProperty("destination_room_index", out var destination) ? destination.GetInt32() : null)).ToArray()
    };
}

public sealed record GodotFeaturePlacement(string DefinitionId, int RoomIndex, int? DestinationRoomIndex = null);

public sealed record SessionFrame(object frame, long tick);
