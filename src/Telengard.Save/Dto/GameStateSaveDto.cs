using Telengard.Core.Combat;
using Telengard.Core.Items;
using Telengard.Core.Knowledge;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Save;

namespace Telengard.Save.Dto;

public sealed record GameStateSaveDto
{
    public int SaveVersion { get; init; }
    public required GameVersionsDto Versions { get; init; }
    public long WorldSeed { get; init; }
    public long SimulationTick { get; init; }
    public GameMode CurrentMode { get; init; }
    public required PlayerStateDto Player { get; init; }
    public required ExpeditionStateDto Expedition { get; init; }
    public required DungeonStateDto Dungeon { get; init; }
    public required KnowledgeStateDto Knowledge { get; init; }
    public required LegacyStateDto Legacy { get; init; }
    public InnStateDto? Inn { get; init; }
    public required SecuredProgressStateDto SecuredProgress { get; init; }
    public required EmptyStateDto Settings { get; init; }
    public CombatStateDto? Combat { get; init; }

    public static GameStateSaveDto FromState(GameState state) => new()
    {
        SaveVersion = state.SaveVersion,
        Versions = GameVersionsDto.FromState(state.Versions),
        WorldSeed = state.WorldSeed,
        SimulationTick = state.SimulationTick,
        CurrentMode = state.CurrentMode,
        Player = PlayerStateDto.FromState(state.Player),
        Expedition = ExpeditionStateDto.FromState(state.Expedition),
        Dungeon = DungeonStateDto.FromState(state.Dungeon),
        Knowledge = KnowledgeStateDto.FromState(state.Knowledge),
        Legacy = LegacyStateDto.FromState(state.Legacy),
        Inn = InnStateDto.FromState(state.Inn),
        SecuredProgress = SecuredProgressStateDto.FromState(state.SecuredProgress),
        Settings = new EmptyStateDto(),
        Combat = CombatStateDto.FromState(state.Combat)
    };

    public GameState ToState()
    {
        SaveMigrations.Validate(this);
        return new GameState
        {
            SaveVersion = SaveVersion,
            Versions = Versions.ToState(),
            WorldSeed = WorldSeed,
            SimulationTick = SimulationTick,
            CurrentMode = CurrentMode,
            Player = Player.ToState(),
            Expedition = Expedition.ToState(),
            Dungeon = Dungeon.ToState(),
            Knowledge = Knowledge.ToState(),
            Legacy = Legacy.ToState(),
            Inn = Inn!.ToState(),
            SecuredProgress = SecuredProgress.ToState(),
            Settings = new SettingsState(),
            Combat = Combat?.ToState()
        };
    }
}

public sealed record GameVersionsDto
{
    public required string SimulationVersion { get; init; }
    public required string GeneratorVersion { get; init; }
    public required string ContentVersion { get; init; }

    public static GameVersionsDto FromState(GameVersions versions) => new()
    {
        SimulationVersion = versions.SimulationVersion,
        GeneratorVersion = versions.GeneratorVersion,
        ContentVersion = versions.ContentVersion
    };

    public GameVersions ToState() => new(SimulationVersion, GeneratorVersion, ContentVersion);
}

public sealed record PlayerStateDto
{
    public Guid Id { get; init; }
    public required PlayerAttributesDto Attributes { get; init; }
    public int Level { get; init; }
    public long Experience { get; init; }
    public int HitPoints { get; init; }
    public int MaxHitPoints { get; init; }
    public int SpellPower { get; init; }
    public int MaxSpellPower { get; init; }
    public required DungeonPositionDto Position { get; init; }
    public required IReadOnlyList<string> Inventory { get; init; }
    public required IReadOnlyList<EquipmentSlotDto> EquipmentSlots { get; init; }
    public required IReadOnlyList<string> Talents { get; init; }
    public required IReadOnlyList<string> Spells { get; init; }
    public required IReadOnlyList<string> Injuries { get; init; }
    public required IReadOnlyList<string> TemporaryEffects { get; init; }
    public int CarriedGold { get; init; }
    public bool Alive { get; init; }

    public static PlayerStateDto FromState(PlayerState player) => new()
    {
        Id = player.Id,
        Attributes = PlayerAttributesDto.FromState(player.Attributes),
        Level = player.Level,
        Experience = player.Experience,
        HitPoints = player.HitPoints,
        MaxHitPoints = player.MaxHitPoints,
        SpellPower = player.SpellPower,
        MaxSpellPower = player.MaxSpellPower,
        Position = DungeonPositionDto.FromState(player.Position),
        Inventory = player.Inventory.ToArray(),
        EquipmentSlots = player.EquipmentSlots.Select(EquipmentSlotDto.FromState).ToArray(),
        Talents = player.Talents.ToArray(),
        Spells = player.Spells.ToArray(),
        Injuries = player.Injuries.ToArray(),
        TemporaryEffects = player.TemporaryEffects.ToArray(),
        CarriedGold = player.CarriedGold,
        Alive = player.Alive
    };

    public PlayerState ToState() => new()
    {
        Id = Id,
        Attributes = Attributes.ToState(),
        Level = Level,
        Experience = Experience,
        HitPoints = HitPoints,
        MaxHitPoints = MaxHitPoints,
        SpellPower = SpellPower,
        MaxSpellPower = MaxSpellPower,
        Position = Position.ToState(),
        Inventory = Inventory.ToArray(),
        EquipmentSlots = EquipmentSlots.Select(slot => slot.ToState()).ToArray(),
        Talents = Talents.ToArray(),
        Spells = Spells.ToArray(),
        Injuries = Injuries.ToArray(),
        TemporaryEffects = TemporaryEffects.ToArray(),
        CarriedGold = CarriedGold,
        Alive = Alive
    };
}


public sealed record PlayerAttributesDto
{
    public int Strength { get; init; }
    public int Intelligence { get; init; }
    public int Wisdom { get; init; }
    public int Constitution { get; init; }
    public int Dexterity { get; init; }
    public int Charisma { get; init; }

    public static PlayerAttributesDto FromState(PlayerAttributes attributes) => new()
    {
        Strength = attributes.Strength,
        Intelligence = attributes.Intelligence,
        Wisdom = attributes.Wisdom,
        Constitution = attributes.Constitution,
        Dexterity = attributes.Dexterity,
        Charisma = attributes.Charisma
    };

    public PlayerAttributes ToState() => new(Strength, Intelligence, Wisdom, Constitution, Dexterity, Charisma);
}

public sealed record DungeonPositionDto
{
    public int Floor { get; init; }
    public int X { get; init; }
    public int Y { get; init; }

    public static DungeonPositionDto FromState(DungeonPosition position) => new() { Floor = position.Floor, X = position.X, Y = position.Y };
    public DungeonPosition ToState() => new(Floor, X, Y);
}

public sealed record PersistentMapStateDto
{
    public required IReadOnlyList<DungeonPositionDto> ObservedPositions { get; init; }
    public required IReadOnlyList<DungeonPositionDto> VisitedPositions { get; init; }

    public static PersistentMapStateDto FromState(PersistentMapState map) => new()
    {
        ObservedPositions = map.ObservedPositions.Select(DungeonPositionDto.FromState).ToArray(),
        VisitedPositions = map.VisitedPositions.Select(DungeonPositionDto.FromState).ToArray()
    };

    public PersistentMapState ToState() => new(
        ObservedPositions.Select(position => position.ToState()),
        VisitedPositions.Select(position => position.ToState()));
}

public sealed record DungeonStateDto
{
    public IReadOnlyList<FeatureInstanceDto>? Features { get; init; }

    public static DungeonStateDto FromState(DungeonState dungeon) => new()
    {
        Features = dungeon.Features.Select(FeatureInstanceDto.FromState).ToArray()
    };

    public DungeonState ToState() => new()
    {
        Features = (Features ?? []).Select(feature => feature.ToState()).ToArray()
    };
}

public sealed record FeatureInstanceDto
{
    public Guid InstanceId { get; init; }
    public required string DefinitionId { get; init; }
    public required DungeonPositionDto Position { get; init; }
    public int ActivationCount { get; init; }
    public bool Discovered { get; init; }

    public static FeatureInstanceDto FromState(FeatureInstance feature) => new()
    {
        InstanceId = feature.InstanceId,
        DefinitionId = feature.DefinitionId,
        Position = DungeonPositionDto.FromState(feature.Position),
        ActivationCount = feature.ActivationCount,
        Discovered = feature.Discovered
    };

    public FeatureInstance ToState() => new(
        InstanceId,
        DefinitionId,
        Position.ToState(),
        ActivationCount,
        Discovered);
}

public sealed record LegacyStateDto
{
    public required PersistentMapStateDto PersistentMap { get; init; }

    public static LegacyStateDto FromState(LegacyState legacy) => new()
    {
        PersistentMap = PersistentMapStateDto.FromState(legacy.PersistentMap)
    };

    public LegacyState ToState() => new() { PersistentMap = PersistentMap.ToState() };
}

public sealed record InnStateDto
{
    public bool IsAtInn { get; init; }

    public static InnStateDto FromState(InnState inn) => new() { IsAtInn = inn.IsAtInn };

    public InnState ToState() => new() { IsAtInn = IsAtInn };
}

public sealed record SecuredProgressStateDto
{
    public int SecuredGold { get; init; }

    public static SecuredProgressStateDto FromState(SecuredProgressState progress) => new()
    {
        SecuredGold = progress.SecuredGold
    };

    public SecuredProgressState ToState() => new() { SecuredGold = SecuredGold };
}

public sealed record KnowledgeStateDto
{
    public IReadOnlyList<KnowledgeEntryDto>? Entries { get; init; }
    public IReadOnlyList<TeleporterMappingDto>? TeleporterMappings { get; init; }

    public static KnowledgeStateDto FromState(KnowledgeState knowledge) => new()
    {
        Entries = knowledge.Entries.Select(KnowledgeEntryDto.FromState).ToArray(),
        TeleporterMappings = knowledge.TeleporterMappings.Select(TeleporterMappingDto.FromState).ToArray()
    };

    public KnowledgeState ToState() => new(
        (Entries ?? []).Select(entry => entry.ToState()),
        (TeleporterMappings ?? []).Select(mapping => mapping.ToState()));
}

public sealed record KnowledgeEntryDto
{
    public required string SubjectId { get; init; }
    public IReadOnlyList<string>? Observations { get; init; }
    public int SampleCount { get; init; }
    public IReadOnlyList<string>? Hypotheses { get; init; }
    public int Confidence { get; init; }
    public IReadOnlyList<string>? ConfirmedFacts { get; init; }

    public static KnowledgeEntryDto FromState(KnowledgeEntry entry) => new()
    {
        SubjectId = entry.SubjectId,
        Observations = entry.Observations.ToArray(),
        SampleCount = entry.SampleCount,
        Hypotheses = entry.Hypotheses.ToArray(),
        Confidence = entry.Confidence,
        ConfirmedFacts = entry.ConfirmedFacts.ToArray()
    };

    public KnowledgeEntry ToState() => new(
        SubjectId,
        Observations ?? [],
        SampleCount,
        Hypotheses ?? [],
        Confidence,
        ConfirmedFacts ?? []);
}

public sealed record TeleporterMappingDto
{
    public required string NetworkId { get; init; }
    public required string NodeId { get; init; }
    public required DungeonPositionDto Source { get; init; }
    public required DungeonPositionDto Destination { get; init; }
    public TeleporterMappingStatus Status { get; init; }

    public static TeleporterMappingDto FromState(TeleporterMapping mapping) => new()
    {
        NetworkId = mapping.NetworkId,
        NodeId = mapping.NodeId,
        Source = DungeonPositionDto.FromState(mapping.Source),
        Destination = DungeonPositionDto.FromState(mapping.Destination),
        Status = mapping.Status
    };

    public TeleporterMapping ToState() => new(
        NetworkId,
        NodeId,
        Source.ToState(),
        Destination.ToState(),
        Status);
}

public sealed record ExpeditionStateDto
{
    public Guid? ExpeditionId { get; init; }
    public int StartingFloor { get; init; }
    public int DeepestFloorReached { get; init; }
    public long StartSimulationTick { get; init; }
    public long SimulationTicks { get; init; }
    public int CarriedGold { get; init; }
    public IReadOnlyList<string>? AcquiredItems { get; init; }
    public int MonstersDefeated { get; init; }
    public IReadOnlyList<string>? DiscoveriesMade { get; init; }
    public IReadOnlyList<int>? FloorsVisited { get; init; }
    public int RoomsVisited { get; init; }
    public IReadOnlyList<string>? Objectives { get; init; }
    public bool Active { get; init; }

    public static ExpeditionStateDto FromState(ExpeditionState expedition) => new()
    {
        ExpeditionId = expedition.ExpeditionId,
        StartingFloor = expedition.StartingFloor,
        DeepestFloorReached = expedition.DeepestFloorReached,
        StartSimulationTick = expedition.StartSimulationTick,
        SimulationTicks = expedition.SimulationTicks,
        CarriedGold = expedition.CarriedGold,
        AcquiredItems = expedition.AcquiredItems.ToArray(),
        MonstersDefeated = expedition.MonstersDefeated,
        DiscoveriesMade = expedition.DiscoveriesMade.ToArray(),
        FloorsVisited = expedition.FloorsVisited.ToArray(),
        RoomsVisited = expedition.RoomsVisited,
        Objectives = expedition.Objectives.ToArray(),
        Active = expedition.Active
    };

    public ExpeditionState ToState() => new()
    {
        ExpeditionId = ExpeditionId,
        StartingFloor = StartingFloor,
        DeepestFloorReached = DeepestFloorReached,
        StartSimulationTick = StartSimulationTick,
        SimulationTicks = SimulationTicks,
        CarriedGold = CarriedGold,
        AcquiredItems = (AcquiredItems ?? []).ToArray(),
        MonstersDefeated = MonstersDefeated,
        DiscoveriesMade = (DiscoveriesMade ?? []).ToArray(),
        FloorsVisited = (FloorsVisited ?? []).ToArray(),
        RoomsVisited = RoomsVisited,
        Objectives = (Objectives ?? []).ToArray(),
        Active = Active
    };
}

public sealed record CombatStateDto
{
    public required MonsterInstanceDto Monster { get; init; }
    public CombatPhase Phase { get; init; }
    public int Round { get; init; }
    public CombatAction? SelectedAction { get; init; }
    public ThreatLevel? ThreatLevel { get; init; }

    public static CombatStateDto? FromState(CombatState? combat) => combat is null ? null : new()
    {
        Monster = MonsterInstanceDto.FromState(combat.Monster),
        Phase = combat.Phase,
        Round = combat.Round,
        SelectedAction = combat.SelectedAction,
        ThreatLevel = combat.ThreatLevel
    };

    public CombatState ToState() => new(Monster.ToState(), Phase, Round, SelectedAction, ThreatLevel);
}

public sealed record MonsterInstanceDto
{
    public Guid InstanceId { get; init; }
    public required string DefinitionId { get; init; }
    public int Level { get; init; }
    public int CurrentHitPoints { get; init; }
    public required DungeonPositionDto Position { get; init; }
    public required IReadOnlyList<string> TemporaryEffects { get; init; }
    public string? CurrentBehaviorState { get; init; }

    public static MonsterInstanceDto FromState(MonsterInstance monster) => new()
    {
        InstanceId = monster.InstanceId,
        DefinitionId = monster.DefinitionId,
        Level = monster.Level,
        CurrentHitPoints = monster.CurrentHitPoints,
        Position = DungeonPositionDto.FromState(monster.Position),
        TemporaryEffects = monster.TemporaryEffects.ToArray(),
        CurrentBehaviorState = monster.CurrentBehaviorState
    };

    public MonsterInstance ToState() => new(
        InstanceId,
        DefinitionId,
        Level,
        CurrentHitPoints,
        Position.ToState(),
        TemporaryEffects.ToArray(),
        CurrentBehaviorState);
}

public sealed record EmptyStateDto;
