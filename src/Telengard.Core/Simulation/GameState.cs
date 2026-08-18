using Telengard.Core.Combat;
using Telengard.Core.Items;
using Telengard.Core.Knowledge;
using Telengard.Core.World.Features;

namespace Telengard.Core.Simulation;

public enum GameMode
{
    Classic,
    Legacy,
    Adventure
}

public sealed record GameVersions
{
    public static GameVersions Current { get; } = new("0.1", "0.1", "0.1");

    public GameVersions(string simulationVersion, string generatorVersion, string contentVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(simulationVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(generatorVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentVersion);
        SimulationVersion = simulationVersion;
        GeneratorVersion = generatorVersion;
        ContentVersion = contentVersion;
    }

    public string SimulationVersion { get; }
    public string GeneratorVersion { get; }
    public string ContentVersion { get; }
}

public sealed record DungeonPosition
{
    public DungeonPosition(int floor, int x, int y)
    {
        if (floor is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(floor), floor, "Floor must be between 1 and 50.");
        }

        Floor = floor;
        X = x;
        Y = y;
    }

    public int Floor { get; }
    public int X { get; }
    public int Y { get; }
}

public sealed record PlayerAttributes(
    int Strength,
    int Intelligence,
    int Wisdom,
    int Constitution,
    int Dexterity,
    int Charisma);

public sealed record PlayerState
{
    private IReadOnlyList<EquipmentSlotState> _equipmentSlots = Array.Empty<EquipmentSlotState>();

    public Guid Id { get; init; }
    public PlayerAttributes Attributes { get; init; } = new(0, 0, 0, 0, 0, 0);
    public int Level { get; init; } = 1;
    public long Experience { get; init; }
    public int HitPoints { get; init; }
    public int MaxHitPoints { get; init; }
    public int SpellPower { get; init; }
    public int MaxSpellPower { get; init; }
    public DungeonPosition Position { get; init; } = new(1, 0, 0);
    public IReadOnlyList<string> Inventory { get; init; } = Array.Empty<string>();
    public IReadOnlyList<EquipmentSlotState> EquipmentSlots
    {
        get => _equipmentSlots;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            var slots = value.ToArray();
            if (slots.Any(slot => slot is null))
            {
                throw new ArgumentException("Equipment slots cannot contain null values.", nameof(value));
            }

            if (slots.Select(slot => slot.SlotId).Distinct(StringComparer.Ordinal).Count() != slots.Length)
            {
                throw new ArgumentException("Equipment slot ids must be unique.", nameof(value));
            }

            var equippedItems = slots
                .Where(slot => slot.ItemInstanceId.HasValue)
                .Select(slot => slot.ItemInstanceId!.Value)
                .ToArray();
            if (equippedItems.Distinct().Count() != equippedItems.Length)
            {
                throw new ArgumentException("An item instance cannot occupy multiple equipment slots.", nameof(value));
            }

            _equipmentSlots = Array.AsReadOnly(slots);
        }
    }
    public IReadOnlyList<string> Talents { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Spells { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Injuries { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TemporaryEffects { get; init; } = Array.Empty<string>();
    public int CarriedGold { get; init; }
    public bool Alive { get; init; } = true;
}

public sealed record DeadHeroRecord(
    Guid HeroId,
    PlayerAttributes Attributes,
    int Level,
    long Experience,
    DungeonPosition DeathPosition,
    Guid? ExpeditionId,
    int DeepestFloorReached);

public sealed record ExpeditionState
{
    public Guid? ExpeditionId { get; init; }
    public int StartingFloor { get; init; } = 1;
    public int DeepestFloorReached { get; init; } = 1;
    public long StartSimulationTick { get; init; }
    public long SimulationTicks { get; init; }
    public int CarriedGold { get; init; }
    public IReadOnlyList<string> AcquiredItems { get; init; } = Array.Empty<string>();
    public int MonstersDefeated { get; init; }
    public IReadOnlyList<string> DiscoveriesMade { get; init; } = Array.Empty<string>();
    public IReadOnlyList<int> FloorsVisited { get; init; } = Array.Empty<int>();
    public int RoomsVisited { get; init; }
    public IReadOnlyList<string> Objectives { get; init; } = Array.Empty<string>();
    public bool Active { get; init; }
}

public sealed record DungeonState
{
    private IReadOnlyList<FeatureInstance> _features = Array.Empty<FeatureInstance>();

    public IReadOnlyList<FeatureInstance> Features
    {
        get => _features;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            var features = value.ToArray();
            if (features.Any(feature => feature is null))
            {
                throw new ArgumentException("Dungeon features cannot contain null values.", nameof(value));
            }

            if (features.Select(feature => feature.InstanceId).Distinct().Count() != features.Length)
            {
                throw new ArgumentException("Dungeon feature instance ids must be unique.", nameof(value));
            }

            _features = Array.AsReadOnly(features);
        }
    }
}

public sealed class PersistentMapState : IEquatable<PersistentMapState>
{
    public PersistentMapState(
        IEnumerable<DungeonPosition>? observedPositions = null,
        IEnumerable<DungeonPosition>? visitedPositions = null)
    {
        VisitedPositions = Normalize(visitedPositions);
        ObservedPositions = Normalize((observedPositions ?? []).Concat(VisitedPositions));
    }

    public IReadOnlyList<DungeonPosition> ObservedPositions { get; }
    public IReadOnlyList<DungeonPosition> VisitedPositions { get; }

    public bool Equals(PersistentMapState? other) => other is not null &&
        ObservedPositions.SequenceEqual(other.ObservedPositions) &&
        VisitedPositions.SequenceEqual(other.VisitedPositions);

    public override bool Equals(object? obj) => Equals(obj as PersistentMapState);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var position in ObservedPositions) hash.Add(position);
        foreach (var position in VisitedPositions) hash.Add(position);
        return hash.ToHashCode();
    }

    private static IReadOnlyList<DungeonPosition> Normalize(IEnumerable<DungeonPosition>? positions) =>
        (positions ?? []).Distinct().OrderBy(position => position.Floor).ThenBy(position => position.X).ThenBy(position => position.Y).ToArray();
}

public sealed record KnowledgeState
{
    private IReadOnlyList<KnowledgeEntry> _entries = Array.Empty<KnowledgeEntry>();
    private IReadOnlyList<TeleporterMapping> _teleporterMappings = Array.Empty<TeleporterMapping>();

    public KnowledgeState(
        IEnumerable<KnowledgeEntry>? entries = null,
        IEnumerable<TeleporterMapping>? teleporterMappings = null)
    {
        Entries = (entries ?? []).ToArray();
        TeleporterMappings = (teleporterMappings ?? []).ToArray();
    }

    public IReadOnlyList<KnowledgeEntry> Entries
    {
        get => _entries;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            var copy = value.ToArray();
            if (copy.Any(entry => entry is null))
            {
                throw new ArgumentException("Knowledge entries cannot contain null values.", nameof(value));
            }

            if (copy.Select(entry => entry.SubjectId).Distinct(StringComparer.Ordinal).Count() != copy.Length)
            {
                throw new ArgumentException("Knowledge entry subject ids must be unique.", nameof(value));
            }

            _entries = Array.AsReadOnly(copy);
        }
    }

    public IReadOnlyList<TeleporterMapping> TeleporterMappings
    {
        get => _teleporterMappings;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            var copy = value.ToArray();
            if (copy.Any(mapping => mapping is null))
            {
                throw new ArgumentException("Teleporter mappings cannot contain null values.", nameof(value));
            }

            if (copy.GroupBy(mapping => (
                    mapping.NetworkId,
                    mapping.NodeId,
                    mapping.Source,
                    mapping.Destination))
                .Any(group => group.Count() > 1))
            {
                throw new ArgumentException("Teleporter mapping relationships must be unique.", nameof(value));
            }

            _teleporterMappings = Array.AsReadOnly(copy);
        }
    }
}

public sealed record LegacyState
{
    private IReadOnlyList<DeadHeroRecord> _previousHeroes = Array.Empty<DeadHeroRecord>();

    public PersistentMapState PersistentMap { get; init; } = new();

    public IReadOnlyList<DeadHeroRecord> PreviousHeroes
    {
        get => _previousHeroes;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            var heroes = value.ToArray();
            if (heroes.Any(hero => hero is null))
            {
                throw new ArgumentException("Previous heroes cannot contain null values.", nameof(value));
            }

            _previousHeroes = Array.AsReadOnly(heroes);
        }
    }
}
public sealed record InnState
{
    public bool IsAtInn { get; init; } = true;
}
public sealed record SecuredProgressState
{
    public int SecuredGold { get; init; }
}
public sealed record SettingsState;

public sealed record GameState
{
    public const int CurrentSaveVersion = 11;

    public int SaveVersion { get; init; } = CurrentSaveVersion;
    public required GameVersions Versions { get; init; }
    public long WorldSeed { get; init; }
    public long SimulationTick { get; init; }
    public GameMode CurrentMode { get; init; }
    public required PlayerState Player { get; init; }
    public required ExpeditionState Expedition { get; init; }
    public required DungeonState Dungeon { get; init; }
    public required KnowledgeState Knowledge { get; init; }
    public required LegacyState Legacy { get; init; }
    public InnState Inn { get; init; } = new();
    public required SecuredProgressState SecuredProgress { get; init; }
    public required SettingsState Settings { get; init; }
    public CombatState? Combat { get; init; }

    public static GameState Create(
        long worldSeed,
        GameVersions? versions = null,
        GameMode mode = GameMode.Classic,
        Guid? playerId = null)
    {
        if (!Enum.IsDefined(typeof(GameMode), mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown game mode.");
        }

        var id = playerId ?? Guid.Empty;
        return new GameState
        {
            Versions = versions ?? GameVersions.Current,
            WorldSeed = worldSeed,
            CurrentMode = mode,
            Player = new PlayerState { Id = id },
            Expedition = new ExpeditionState(),
            Dungeon = new DungeonState(),
            Knowledge = new KnowledgeState(),
            Legacy = new LegacyState { PersistentMap = new PersistentMapState() },
            Inn = new InnState(),
            SecuredProgress = new SecuredProgressState(),
            Settings = new SettingsState()
        };
    }
}
