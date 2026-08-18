using Telengard.Save.Dto;

namespace Telengard.Save;

public static class SaveMigrations
{
    public const int CurrentSaveVersion = 13;

    public static GameStateSaveDto Migrate(GameStateSaveDto save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.SaveVersion is < 1 or > CurrentSaveVersion)
        {
            throw new SaveFormatException($"Unsupported save version: {save.SaveVersion}.");
        }

        var migrated = save;
        if (save.SaveVersion <= 1) migrated = MigrateVersionOne(migrated);
        if (save.SaveVersion <= 2) migrated = MigrateVersionTwo(migrated);
        if (save.SaveVersion <= 4) migrated = MigrateVersionFour(migrated);
        if (save.SaveVersion <= 4) migrated = MigrateVersionFive(migrated);
        if (save.SaveVersion <= 6) migrated = MigrateVersionSix(migrated);
        if (save.SaveVersion <= 7) migrated = MigrateVersionSeven(migrated);
        if (save.SaveVersion <= 8) migrated = MigrateVersionEight(migrated);
        if (save.SaveVersion <= 9) migrated = MigrateVersionNine(migrated);
        if (save.SaveVersion <= 10) migrated = MigrateVersionTen(migrated);
        if (save.SaveVersion <= 11) migrated = MigrateVersionEleven(migrated);
        if (save.SaveVersion <= 12) migrated = MigrateVersionTwelve(migrated);
        if (save.SaveVersion <= 13) migrated = MigrateVersionThirteen(migrated);
        return migrated;
    }

    public static void Validate(GameStateSaveDto save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.SaveVersion != CurrentSaveVersion)
        {
            throw new SaveFormatException($"Unsupported save version: {save.SaveVersion}.");
        }

        if (save.Versions is null || save.Player is null || save.Expedition is null ||
            save.Dungeon is null || save.Knowledge is null || save.Legacy is null ||
            save.Inn is null || save.SecuredProgress is null ||
            save.Settings is null ||
            !Enum.IsDefined(save.CurrentMode))
        {
            throw new SaveFormatException("Save document is missing required state.");
        }

        if (save.Player.Attributes is null || save.Player.Position is null ||
            save.Player.Inventory is null || save.Player.EquipmentSlots is null ||
            save.Player.Talents is null || save.Player.Spells is null ||
            save.Player.Injuries is null || save.Player.TemporaryEffects is null ||
            save.Expedition.AcquiredItems is null || save.Expedition.DiscoveriesMade is null ||
            save.Expedition.FloorsVisited is null ||
            save.Player.EquipmentSlots.Any(slot => slot is null ||
                string.IsNullOrWhiteSpace(slot.SlotId) ||
                slot.ItemInstanceId == Guid.Empty) ||
            save.Player.EquipmentSlots.Select(slot => slot.SlotId).Distinct(StringComparer.Ordinal).Count() !=
                save.Player.EquipmentSlots.Count ||
            save.Player.EquipmentSlots.Where(slot => slot.ItemInstanceId.HasValue)
                .Select(slot => slot.ItemInstanceId!.Value).Distinct().Count() !=
                save.Player.EquipmentSlots.Count(slot => slot.ItemInstanceId.HasValue) ||
            save.Knowledge.Entries is null ||
            save.Knowledge.Entries.Any(entry => entry is null ||
                string.IsNullOrWhiteSpace(entry.SubjectId) ||
                entry.SampleCount < 0 ||
                entry.Confidence is < 0 or > 100 ||
                !HasValidValues(entry.Observations) ||
                !HasValidValues(entry.Hypotheses) ||
                !HasValidValues(entry.ConfirmedFacts)) ||
            save.Knowledge.Entries.Select(entry => entry.SubjectId).Distinct(StringComparer.Ordinal).Count() != save.Knowledge.Entries.Count ||
            save.Knowledge.TeleporterMappings is null ||
            save.Knowledge.TeleporterMappings.Any(mapping => mapping is null ||
                string.IsNullOrWhiteSpace(mapping.NetworkId) ||
                string.IsNullOrWhiteSpace(mapping.NodeId) ||
                mapping.Source is null ||
                mapping.Destination is null ||
                !Enum.IsDefined(mapping.Status) ||
                mapping.Status == Telengard.Core.Knowledge.TeleporterMappingStatus.Unknown) ||
            save.Knowledge.TeleporterMappings.GroupBy(mapping =>
                (mapping.NetworkId, mapping.NodeId, mapping.Source?.Floor, mapping.Source?.X, mapping.Source?.Y,
                    mapping.Destination?.Floor, mapping.Destination?.X, mapping.Destination?.Y))
                .Any(group => group.Count() > 1))
        {
            throw new SaveFormatException("Save document is missing collection or player state.");
        }

        if (save.Dungeon.Features is null || save.Dungeon.Features.Any(feature =>
                feature is null || feature.InstanceId == Guid.Empty || string.IsNullOrWhiteSpace(feature.DefinitionId) ||
                feature.Position is null || feature.ActivationCount < 0) ||
            save.Dungeon.Features.Select(feature => feature.InstanceId).Distinct().Count() != save.Dungeon.Features.Count)
        {
            throw new SaveFormatException("Save document contains invalid dungeon feature state.");
        }

        if (save.Legacy.PersistentMap is null || save.Legacy.PersistentMap.ObservedPositions is null ||
            save.Legacy.PersistentMap.VisitedPositions is null || save.Legacy.PreviousHeroes is null ||
            save.Legacy.PreviousHeroes.Any(hero => hero is null || hero.Attributes is null || hero.DeathPosition is null) ||
            save.Legacy.Graves is null || save.Legacy.Graves.Any(grave => grave is null || grave.Position is null) ||
            save.Legacy.Heirlooms is null || save.Legacy.Heirlooms.Any(heirloom =>
                heirloom is null || heirloom.HeroId == Guid.Empty || string.IsNullOrWhiteSpace(heirloom.ItemId)))
        {
            throw new SaveFormatException("Save document is missing legacy state.");
        }

        var observed = save.Legacy.PersistentMap.ObservedPositions.Select(position => (position.Floor, position.X, position.Y)).ToHashSet();
        if (save.Legacy.PersistentMap.VisitedPositions.Any(position => !observed.Contains((position.Floor, position.X, position.Y))))
        {
            throw new SaveFormatException("Visited map positions must also be observed.");
        }

        if (save.Combat is not null &&
            (save.Combat.Monster is null || save.Combat.Monster.Position is null ||
             save.Combat.Monster.TemporaryEffects is null ||
             !Enum.IsDefined(save.Combat.Phase) ||
             (save.Combat.SelectedAction is not null && !Enum.IsDefined(save.Combat.SelectedAction.Value)) ||
             (save.Combat.ThreatLevel is not null && !Enum.IsDefined(save.Combat.ThreatLevel.Value))))
        {
            throw new SaveFormatException("Save document contains invalid combat state.");
        }
    }

    private static PersistentMapStateDto EmptyMap() => new() { ObservedPositions = [], VisitedPositions = [] };

    private static GameStateSaveDto MigrateVersionOne(GameStateSaveDto save) => save with
    {
        SaveVersion = 2,
        Expedition = MigrateVersionOneExpedition(save.Expedition),
        Legacy = new LegacyStateDto { PersistentMap = EmptyMap() }
    };

    private static GameStateSaveDto MigrateVersionTwo(GameStateSaveDto save) => save with
    {
        SaveVersion = 3,
        Inn = new InnStateDto { IsAtInn = true }
    };

    private static GameStateSaveDto MigrateVersionFour(GameStateSaveDto save) => save with
    {
        SaveVersion = 4,
        SecuredProgress = new SecuredProgressStateDto()
    };

    private static GameStateSaveDto MigrateVersionFive(GameStateSaveDto save) => save with
    {
        SaveVersion = 5,
        Combat = null
    };

    private static GameStateSaveDto MigrateVersionSix(GameStateSaveDto save) => save with
    {
        SaveVersion = 6
    };

    private static GameStateSaveDto MigrateVersionSeven(GameStateSaveDto save) => save with
    {
        SaveVersion = 7,
        Dungeon = new DungeonStateDto { Features = save.Dungeon?.Features ?? [] }
    };

    private static GameStateSaveDto MigrateVersionEight(GameStateSaveDto save) => save with
    {
        SaveVersion = 8,
        Knowledge = new KnowledgeStateDto { Entries = save.Knowledge?.Entries ?? [] }
    };

    private static GameStateSaveDto MigrateVersionNine(GameStateSaveDto save) => save with
    {
        SaveVersion = 9,
        Knowledge = new KnowledgeStateDto
        {
            Entries = save.Knowledge?.Entries ?? [],
            TeleporterMappings = save.Knowledge?.TeleporterMappings ?? []
        }
    };

    private static GameStateSaveDto MigrateVersionTen(GameStateSaveDto save) => save with
    {
        SaveVersion = CurrentSaveVersion
    };

    private static GameStateSaveDto MigrateVersionEleven(GameStateSaveDto save) => save with
    {
        SaveVersion = CurrentSaveVersion,
        Legacy = save.Legacy is null
            ? new LegacyStateDto { PersistentMap = EmptyMap(), PreviousHeroes = [] }
            : save.Legacy with { PreviousHeroes = save.Legacy.PreviousHeroes ?? [] }
    };

    private static GameStateSaveDto MigrateVersionTwelve(GameStateSaveDto save) => save with
    {
        SaveVersion = CurrentSaveVersion,
        Legacy = save.Legacy is null
            ? new LegacyStateDto { PersistentMap = EmptyMap(), PreviousHeroes = [], Graves = [] }
            : save.Legacy with { Graves = save.Legacy.Graves ?? [] }
    };

    private static GameStateSaveDto MigrateVersionThirteen(GameStateSaveDto save) => save with
    {
        SaveVersion = CurrentSaveVersion,
        Legacy = save.Legacy is null
            ? new LegacyStateDto { PersistentMap = EmptyMap(), PreviousHeroes = [], Graves = [], Heirlooms = [] }
            : save.Legacy with { Heirlooms = save.Legacy.Heirlooms ?? [] }
    };

    private static bool HasValidValues(IReadOnlyList<string>? values) =>
        values is not null &&
        values.All(value => !string.IsNullOrWhiteSpace(value)) &&
        values.Distinct(StringComparer.Ordinal).Count() == values.Count;

    private static ExpeditionStateDto MigrateVersionOneExpedition(ExpeditionStateDto expedition)
    {
        var floorsVisited = expedition.FloorsVisited ?? [];
        return expedition with
        {
            StartingFloor = expedition.StartingFloor == 0 ? floorsVisited.DefaultIfEmpty(1).First() : expedition.StartingFloor,
            DeepestFloorReached = expedition.DeepestFloorReached == 0 ? floorsVisited.DefaultIfEmpty(1).Max() : expedition.DeepestFloorReached,
            AcquiredItems = expedition.AcquiredItems ?? [],
            DiscoveriesMade = expedition.DiscoveriesMade ?? [],
            FloorsVisited = floorsVisited,
            Objectives = expedition.Objectives ?? []
        };
    }
}

public sealed class SaveFormatException : Exception
{
    public SaveFormatException(string message) : base(message) { }
    public SaveFormatException(string message, Exception innerException) : base(message, innerException) { }
}
