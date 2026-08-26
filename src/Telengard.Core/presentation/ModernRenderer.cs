using Telengard.Core.Combat;
using Telengard.Core.Economy;
using Telengard.Core.Items;
using Telengard.Core.Magic;
using Telengard.Core.Knowledge;
using Telengard.Core.Progression;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Core.World.Generation;

namespace Telengard.Core.Presentation;

/// <summary>
/// Builds the renderer input for the Modern presentation from committed
/// simulation state and domain events. It does not own or mutate game state.
/// </summary>
public static class ModernRenderer
{
    public static ModernRenderFrame Create(
        PresentationState state,
        IEnumerable<IDomainEvent>? committedEvents = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        var events = committedEvents?.ToArray() ?? [];
        if (events.Any(domainEvent => domainEvent is null))
        {
            throw new ArgumentException("Committed events cannot contain null values.", nameof(committedEvents));
        }

        return new ModernRenderFrame(
            state.IsAtInn ? ModernScene.Inn : ModernScene.Dungeon,
            state.Player.Position,
            new ModernEnvironment(
                DynamicLighting: !state.IsAtInn,
                AtmosphericEffects: !state.IsAtInn,
                ThemeId: state.IsAtInn ? "inn" : "dungeon"),
            CreateTiles(state),
            state.DiscoveredFeatures
                .OrderBy(feature => feature.Position.Floor)
                .ThenBy(feature => feature.Position.X)
                .ThenBy(feature => feature.Position.Y)
                .ThenBy(feature => feature.InstanceId)
                .Select(feature => new ModernFeatureMarker(
                    feature.InstanceId,
                    feature.DefinitionId,
                    feature.Position,
                    feature.ActivationCount))
                .ToArray(),
            new ModernHud(
                state.Player.Id,
                state.Player.Level,
                state.Player.HitPoints,
                state.Player.MaxHitPoints,
                state.Player.SpellPower,
                state.Player.MaxSpellPower,
                state.Player.CarriedGold,
                state.SecuredGold,
                state.Player.Alive),
            state.Combat is null ? null : new ModernCombatOverlay(state.Combat),
            events.Select(CreateCue).Where(cue => cue is not null).Cast<ModernCue>().ToArray(),
            state.Player.Inventory,
            state.Player.Spells,
            state.Knowledge.Select(entry => entry.SubjectId),
            state.Player.EquipmentSlots.Select(slot => new ModernEquipmentSlot(
                slot.SlotId,
                slot.ItemInstanceId)));
    }

    private static IReadOnlyList<ModernTileMarker> CreateTiles(PresentationState state)
    {
        var tiles = new Dictionary<DungeonPosition, ModernTileKnowledge>();

        foreach (var position in state.ObservedMap)
        {
            tiles[position] = ModernTileKnowledge.Observed;
        }

        foreach (var position in state.VisitedMap)
        {
            tiles[position] = ModernTileKnowledge.Visited;
        }

        tiles[state.Player.Position] = ModernTileKnowledge.Current;

        return tiles
            .OrderBy(pair => pair.Key.Floor)
            .ThenBy(pair => pair.Key.X)
            .ThenBy(pair => pair.Key.Y)
            .Select(pair => new ModernTileMarker(pair.Key, pair.Value, Connections(pair.Key, tiles.Keys)))
            .ToArray();
    }

    private static ModernTileConnections Connections(
        DungeonPosition position,
        IEnumerable<DungeonPosition> knownPositions)
    {
        var known = knownPositions.ToHashSet();
        var connections = ModernTileConnections.None;
        if (known.Contains(new DungeonPosition(position.Floor, position.X, position.Y - 1))) connections |= ModernTileConnections.North;
        if (known.Contains(new DungeonPosition(position.Floor, position.X, position.Y + 1))) connections |= ModernTileConnections.South;
        if (known.Contains(new DungeonPosition(position.Floor, position.X + 1, position.Y))) connections |= ModernTileConnections.East;
        if (known.Contains(new DungeonPosition(position.Floor, position.X - 1, position.Y))) connections |= ModernTileConnections.West;
        return connections;
    }

    private static ModernCue? CreateCue(IDomainEvent domainEvent) => domainEvent switch
    {
        DungeonEnteredEvent entered => new ModernCue(ModernCueKind.DungeonEntered, entered.Position),
        PlayerMovedEvent moved => new ModernCue(ModernCueKind.PlayerMoved, moved.To),
        FloorChangedEvent floorChanged => new ModernCue(ModernCueKind.FloorChanged, floorChanged.To),
        DungeonLeftEvent left => new ModernCue(ModernCueKind.DungeonLeft, left.Position),
        FeatureDiscoveredEvent discovered => new ModernCue(
            ModernCueKind.FeatureDiscovered,
            discovered.Position,
            discovered.FeatureId),
        FeatureActivatedEvent activated => new ModernCue(
            ModernCueKind.FeatureActivated,
            activated.Position,
            activated.FeatureId,
            activated.ActivationCount),
        FeatureOutcomeResolvedEvent outcome => new ModernCue(
            ModernCueKind.FeatureOutcomeResolved,
            outcome.Position,
            outcome.FeatureId,
            outcome.ActivationCount,
            outcome.Effects.Concat(outcome.Observations)),
        FountainOutcomeResolvedEvent outcome => new ModernCue(
            ModernCueKind.FeatureOutcomeResolved,
            outcome.Position,
            outcome.FeatureId,
            outcome.ActivationCount,
            outcome.Effects.Concat(outcome.Observations)),
        AltarOutcomeResolvedEvent outcome => new ModernCue(
            ModernCueKind.FeatureOutcomeResolved,
            outcome.Position,
            outcome.FeatureId,
            outcome.ActivationCount,
            outcome.Effects.Concat(outcome.Observations)),
        PitOutcomeResolvedEvent outcome => new ModernCue(
            ModernCueKind.FeatureOutcomeResolved,
            outcome.Position,
            outcome.FeatureId,
            outcome.ActivationCount,
            outcome.Effects.Concat(outcome.Observations)),
        TeleporterOutcomeResolvedEvent outcome => new ModernCue(
            ModernCueKind.FeatureOutcomeResolved,
            outcome.From,
            outcome.FeatureId,
            outcome.ActivationCount,
            outcome.Effects.Concat(outcome.Observations)),
        EncounterStartedEvent => new ModernCue(ModernCueKind.CombatStarted),
        EncounterEndedEvent ended => new ModernCue(ModernCueKind.CombatEnded, EntityId: ended.EncounterId),
        CombatPhaseChangedEvent phaseChanged => new ModernCue(
            ModernCueKind.CombatPhaseChanged,
            EntityId: phaseChanged.EncounterId,
            Value: (int)phaseChanged.To),
        MonsterDamagedEvent damaged => new ModernCue(
            ModernCueKind.MonsterDamaged,
            EntityId: damaged.MonsterInstanceId,
            Value: damaged.Amount),
        MonsterKilledEvent killed => new ModernCue(ModernCueKind.MonsterKilled, EntityId: killed.MonsterInstanceId),
        SpellCastEvent spellCast => new ModernCue(
            ModernCueKind.SpellCast,
            EntityId: spellCast.EncounterId),
        ItemIdentifiedEvent identified => new ModernCue(ModernCueKind.ItemIdentified, EntityId: identified.ItemId),
        ItemEquippedEvent equipped => new ModernCue(ModernCueKind.ItemEquipped, EntityId: equipped.ItemInstanceId),
        ItemUnequippedEvent unequipped => new ModernCue(ModernCueKind.ItemUnequipped, EntityId: unequipped.ItemInstanceId),
        GoldAcquiredEvent gold => new ModernCue(ModernCueKind.GoldAcquired, Value: gold.Amount),
        GoldSecuredEvent secured => new ModernCue(ModernCueKind.GoldSecured, Value: secured.Amount),
        PlayerLeveledUpEvent level => new ModernCue(ModernCueKind.PlayerLeveledUp, Value: level.Level),
        ExperienceAwardedEvent => new ModernCue(ModernCueKind.ExperienceAwarded),
        PlayerDiedEvent died => new ModernCue(ModernCueKind.PlayerDied, died.Position),
        ExpeditionSucceededEvent => new ModernCue(ModernCueKind.ExpeditionSucceeded),
        ExpeditionFailedEvent => new ModernCue(ModernCueKind.ExpeditionFailed),
        KnowledgeObservationAddedEvent => new ModernCue(ModernCueKind.KnowledgeUpdated),
        KnowledgeSampleCountedEvent => new ModernCue(ModernCueKind.KnowledgeUpdated),
        KnowledgeConfidenceUpdatedEvent => new ModernCue(ModernCueKind.KnowledgeUpdated),
        _ => null
    };
}

public enum ModernScene
{
    Inn,
    Dungeon
}

public enum ModernTileKnowledge
{
    Observed,
    Visited,
    Current
}

public enum ModernCueKind
{
    DungeonEntered,
    PlayerMoved,
    FloorChanged,
    DungeonLeft,
    FeatureDiscovered,
    FeatureActivated,
    FeatureOutcomeResolved,
    CombatStarted,
    CombatEnded,
    CombatPhaseChanged,
    MonsterDamaged,
    MonsterKilled,
    SpellCast,
    ItemIdentified,
    ItemEquipped,
    ItemUnequipped,
    GoldAcquired,
    GoldSecured,
    PlayerLeveledUp,
    ExperienceAwarded,
    PlayerDied,
    ExpeditionSucceeded,
    ExpeditionFailed,
    KnowledgeUpdated
}

public sealed record ModernEnvironment(
    bool DynamicLighting,
    bool AtmosphericEffects,
    string ThemeId = "dungeon");

public sealed record ModernTileMarker(
    DungeonPosition Position,
    ModernTileKnowledge Knowledge,
    ModernTileConnections Connections = ModernTileConnections.None);

[Flags]
public enum ModernTileConnections
{
    None = 0,
    North = 1,
    South = 2,
    East = 4,
    West = 8
}

public sealed record ModernFeatureMarker(
    Guid InstanceId,
    string DefinitionId,
    DungeonPosition Position,
    int ActivationCount)
{
    public string PresentationKey => DefinitionId;
}

public sealed record ModernHud(
    Guid PlayerId,
    int Level,
    int HitPoints,
    int MaxHitPoints,
    int SpellPower,
    int MaxSpellPower,
    int CarriedGold,
    int SecuredGold,
    bool Alive);

public sealed record ModernCombatOverlay
{
    public ModernCombatOverlay(PresentationCombatState combat)
    {
        ArgumentNullException.ThrowIfNull(combat);

        EncounterId = combat.EncounterId;
        Phase = combat.Phase;
        Round = combat.Round;
        ThreatLevel = combat.ThreatLevel;
        Monster = new ModernMonsterMarker(combat.Monster);
    }

    public Guid EncounterId { get; }
    public CombatPhase Phase { get; }
    public int Round { get; }
    public ThreatLevel? ThreatLevel { get; }
    public ModernMonsterMarker Monster { get; }
}

public sealed record ModernMonsterMarker
{
    public ModernMonsterMarker(PresentationMonsterState monster)
    {
        ArgumentNullException.ThrowIfNull(monster);

        InstanceId = monster.InstanceId;
        DefinitionId = monster.DefinitionId;
        Position = monster.Position;
    }

    public Guid InstanceId { get; }
    public string DefinitionId { get; }
    public DungeonPosition Position { get; }
    public string PresentationKey => DefinitionId;
}

public sealed record ModernCue
{
    public ModernCue(
        ModernCueKind Kind,
        DungeonPosition? Position = null,
        Guid? EntityId = null,
        int? Value = null,
        IEnumerable<string>? Details = null)
    {
        this.Kind = Kind;
        this.Position = Position;
        this.EntityId = EntityId;
        this.Value = Value;
        this.Details = Details is null
            ? null
            : Array.AsReadOnly(Details.ToArray());
    }

    public ModernCueKind Kind { get; }
    public DungeonPosition? Position { get; }
    public Guid? EntityId { get; }
    public int? Value { get; }
    public IReadOnlyList<string>? Details { get; }
}

public sealed record ModernEquipmentSlot(string SlotId, Guid? ItemInstanceId)
{
    public bool Equipped => ItemInstanceId.HasValue;
}

public sealed record ModernRenderFrame
{
    public ModernRenderFrame(
        ModernScene scene,
        DungeonPosition playerPosition,
        ModernEnvironment environment,
        IEnumerable<ModernTileMarker> tiles,
        IEnumerable<ModernFeatureMarker> features,
        ModernHud hud,
        ModernCombatOverlay? combat,
        IEnumerable<ModernCue> cues,
        IEnumerable<string>? inventory = null,
        IEnumerable<string>? spells = null,
        IEnumerable<string>? journal = null,
        IEnumerable<ModernEquipmentSlot>? equipment = null)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(hud);
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(cues);

        Scene = scene;
        PlayerPosition = playerPosition ?? throw new ArgumentNullException(nameof(playerPosition));
        Environment = environment;
        Tiles = Copy(tiles, nameof(tiles));
        Features = Copy(features, nameof(features));
        Hud = hud;
        Combat = combat;
        Cues = Copy(cues, nameof(cues));
        Inventory = Copy(inventory ?? [], nameof(inventory));
        Spells = Copy(spells ?? [], nameof(spells));
        Journal = Copy(journal ?? [], nameof(journal));
        Equipment = Copy(equipment ?? [], nameof(equipment));
    }

    public ModernScene Scene { get; }
    public DungeonPosition PlayerPosition { get; }
    public ModernEnvironment Environment { get; }
    public IReadOnlyList<ModernTileMarker> Tiles { get; }
    public IReadOnlyList<ModernFeatureMarker> Features { get; }
    public ModernHud Hud { get; }
    public ModernCombatOverlay? Combat { get; }
    public IReadOnlyList<ModernCue> Cues { get; }
    public IReadOnlyList<string> Inventory { get; }
    public IReadOnlyList<string> Spells { get; }
    public IReadOnlyList<string> Journal { get; }
    public IReadOnlyList<ModernEquipmentSlot> Equipment { get; }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values, string parameterName)
    {
        var copy = values.ToArray();
        if (copy.Any(value => value is null))
        {
            throw new ArgumentException("Modern render collections cannot contain null values.", parameterName);
        }

        return Array.AsReadOnly(copy);
    }
}
