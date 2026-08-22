using Telengard.Core.Combat;
using Telengard.Core.Items;
using Telengard.Core.Knowledge;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;

namespace Telengard.Core.Presentation;

/// <summary>
/// Projects authoritative simulation state into the renderer-facing state shape.
/// </summary>
public static class PresentationStateAdapter
{
    public static PresentationState Create(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new PresentationState(
            state.CurrentMode,
            state.Versions,
            state.SimulationTick,
            state.Inn.IsAtInn,
            state.SecuredProgress.SecuredGold,
            new PresentationPlayerState(state.Player),
            new PresentationExpeditionState(state.Expedition),
            state.Legacy.PersistentMap.ObservedPositions,
            state.Legacy.PersistentMap.VisitedPositions,
            state.Dungeon.Features
                .Where(feature => feature.Discovered)
                .Select(feature => new PresentationFeatureState(feature))
                .ToArray(),
            state.Knowledge.Entries,
            state.Combat is null ? null : new PresentationCombatState(state.Combat));
    }
}

public sealed record PresentationState
{
    public PresentationState(
        GameMode mode,
        GameVersions versions,
        long simulationTick,
        bool isAtInn,
        int securedGold,
        PresentationPlayerState player,
        PresentationExpeditionState expedition,
        IEnumerable<DungeonPosition> observedMap,
        IEnumerable<DungeonPosition> visitedMap,
        IEnumerable<PresentationFeatureState> discoveredFeatures,
        IEnumerable<KnowledgeEntry> knowledge,
        PresentationCombatState? combat)
    {
        Versions = versions ?? throw new ArgumentNullException(nameof(versions));
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Expedition = expedition ?? throw new ArgumentNullException(nameof(expedition));
        ObservedMap = Copy(observedMap, nameof(observedMap));
        VisitedMap = Copy(visitedMap, nameof(visitedMap));
        DiscoveredFeatures = Copy(discoveredFeatures, nameof(discoveredFeatures));
        Knowledge = Copy(knowledge, nameof(knowledge));
        Mode = mode;
        SimulationTick = simulationTick;
        IsAtInn = isAtInn;
        SecuredGold = securedGold;
        Combat = combat;
    }

    public GameMode Mode { get; }
    public GameVersions Versions { get; }
    public long SimulationTick { get; }
    public bool IsAtInn { get; }
    public int SecuredGold { get; }
    public PresentationPlayerState Player { get; }
    public PresentationExpeditionState Expedition { get; }
    public IReadOnlyList<DungeonPosition> ObservedMap { get; }
    public IReadOnlyList<DungeonPosition> VisitedMap { get; }
    public IReadOnlyList<PresentationFeatureState> DiscoveredFeatures { get; }
    public IReadOnlyList<KnowledgeEntry> Knowledge { get; }
    public PresentationCombatState? Combat { get; }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.ToArray();
        if (copy.Any(value => value is null))
        {
            throw new ArgumentException("Presentation collections cannot contain null values.", parameterName);
        }

        return Array.AsReadOnly(copy);
    }
}

public sealed record PresentationPlayerState
{
    public PresentationPlayerState(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        Id = player.Id;
        Attributes = player.Attributes;
        Level = player.Level;
        Experience = player.Experience;
        HitPoints = player.HitPoints;
        MaxHitPoints = player.MaxHitPoints;
        SpellPower = player.SpellPower;
        MaxSpellPower = player.MaxSpellPower;
        Position = player.Position;
        Inventory = Copy(player.Inventory);
        EquipmentSlots = Copy(player.EquipmentSlots);
        Talents = Copy(player.Talents);
        Spells = Copy(player.Spells);
        Injuries = Copy(player.Injuries);
        TemporaryEffects = Copy(player.TemporaryEffects);
        CarriedGold = player.CarriedGold;
        Alive = player.Alive;
    }

    public Guid Id { get; }
    public PlayerAttributes Attributes { get; }
    public int Level { get; }
    public long Experience { get; }
    public int HitPoints { get; }
    public int MaxHitPoints { get; }
    public int SpellPower { get; }
    public int MaxSpellPower { get; }
    public DungeonPosition Position { get; }
    public IReadOnlyList<string> Inventory { get; }
    public IReadOnlyList<EquipmentSlotState> EquipmentSlots { get; }
    public IReadOnlyList<string> Talents { get; }
    public IReadOnlyList<string> Spells { get; }
    public IReadOnlyList<string> Injuries { get; }
    public IReadOnlyList<string> TemporaryEffects { get; }
    public int CarriedGold { get; }
    public bool Alive { get; }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Array.AsReadOnly(values.ToArray());
    }
}

public sealed record PresentationExpeditionState
{
    public PresentationExpeditionState(ExpeditionState expedition)
    {
        ArgumentNullException.ThrowIfNull(expedition);

        ExpeditionId = expedition.ExpeditionId;
        StartingFloor = expedition.StartingFloor;
        DeepestFloorReached = expedition.DeepestFloorReached;
        StartSimulationTick = expedition.StartSimulationTick;
        SimulationTicks = expedition.SimulationTicks;
        CarriedGold = expedition.CarriedGold;
        AcquiredItems = Copy(expedition.AcquiredItems);
        MonstersDefeated = expedition.MonstersDefeated;
        DiscoveriesMade = Copy(expedition.DiscoveriesMade);
        FloorsVisited = Copy(expedition.FloorsVisited);
        RoomsVisited = expedition.RoomsVisited;
        Objectives = Copy(expedition.Objectives);
        Active = expedition.Active;
    }

    public Guid? ExpeditionId { get; }
    public int StartingFloor { get; }
    public int DeepestFloorReached { get; }
    public long StartSimulationTick { get; }
    public long SimulationTicks { get; }
    public int CarriedGold { get; }
    public IReadOnlyList<string> AcquiredItems { get; }
    public int MonstersDefeated { get; }
    public IReadOnlyList<string> DiscoveriesMade { get; }
    public IReadOnlyList<int> FloorsVisited { get; }
    public int RoomsVisited { get; }
    public IReadOnlyList<string> Objectives { get; }
    public bool Active { get; }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Array.AsReadOnly(values.ToArray());
    }
}

public sealed record PresentationFeatureState
{
    public PresentationFeatureState(FeatureInstance feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        if (!feature.Discovered)
        {
            throw new ArgumentException("Only discovered features can be projected.", nameof(feature));
        }

        InstanceId = feature.InstanceId;
        DefinitionId = feature.DefinitionId;
        Position = feature.Position;
        ActivationCount = feature.ActivationCount;
    }

    public Guid InstanceId { get; }
    public string DefinitionId { get; }
    public DungeonPosition Position { get; }
    public int ActivationCount { get; }
}

public sealed record PresentationCombatState
{
    public PresentationCombatState(CombatState combat)
    {
        ArgumentNullException.ThrowIfNull(combat);

        EncounterId = combat.EncounterId;
        Phase = combat.Phase;
        Round = combat.Round;
        SelectedAction = combat.SelectedAction;
        ThreatLevel = combat.ThreatLevel;
        Monster = new PresentationMonsterState(combat.Monster);
    }

    public Guid EncounterId { get; }
    public CombatPhase Phase { get; }
    public int Round { get; }
    public CombatAction? SelectedAction { get; }
    public ThreatLevel? ThreatLevel { get; }
    public PresentationMonsterState Monster { get; }
}

public sealed record PresentationMonsterState
{
    public PresentationMonsterState(MonsterInstance monster)
    {
        ArgumentNullException.ThrowIfNull(monster);

        InstanceId = monster.InstanceId;
        DefinitionId = monster.DefinitionId;
        CurrentHitPoints = monster.CurrentHitPoints;
        Position = monster.Position;
    }

    public Guid InstanceId { get; }
    public string DefinitionId { get; }
    public int CurrentHitPoints { get; }
    public DungeonPosition Position { get; }
}
