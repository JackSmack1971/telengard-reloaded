using Telengard.Core.Simulation;
using Telengard.Core.Combat;
using Telengard.Save;
using Telengard.Save.Dto;
using System.Text.Json.Nodes;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class SaveGameSerializerTests
{
    [Fact]
    public void Serialize_and_deserialize_preserves_authoritative_state()
    {
        var state = GameState.Create(1234, mode: GameMode.Legacy) with
        {
            SimulationTick = 42,
            Legacy = new LegacyState
            {
                PersistentMap = new PersistentMapState(
                    [new DungeonPosition(1, 3, 4), new DungeonPosition(7, 8, 9)],
                    [new DungeonPosition(7, 8, 9)]),
                PreviousHeroes =
                [
                    new DeadHeroRecord(
                        Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        new PlayerAttributes(7, 8, 9, 10, 11, 12),
                        4,
                        81,
                        new DungeonPosition(4, 5, 6),
                        Guid.Parse("00000000-0000-0000-0000-000000000003"),
                        4)
                ],
                Graves =
                [
                    new GraveRecord(
                        Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        new DungeonPosition(4, 5, 6),
                        Guid.Parse("00000000-0000-0000-0000-000000000003"))
                ],
                Heirlooms =
                [
                    new HeirloomRecord(
                        Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        "ember-blade")
                ]
            },
            Player = new PlayerState
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Attributes = new(1, 2, 3, 4, 5, 6),
                Position = new(7, 8, 9),
                Inventory = ["potion"],
                CarriedGold = 17,
                Alive = true
            },
            Expedition = new ExpeditionState { Active = true, CarriedGold = 17, FloorsVisited = [1, 7] },
            SecuredProgress = new SecuredProgressState { SecuredGold = 23 }
        };

        var roundTrip = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(
            SaveGameSerializer.Serialize(state),
            SaveGameSerializer.Serialize(roundTrip));
        Assert.Equal(state.SaveVersion, roundTrip.SaveVersion);
        Assert.Equal(state.Versions, roundTrip.Versions);
        Assert.Equal(state.WorldSeed, roundTrip.WorldSeed);
        Assert.Equal(state.SimulationTick, roundTrip.SimulationTick);
        Assert.Equal(state.CurrentMode, roundTrip.CurrentMode);
        Assert.Equal(state.Player.Id, roundTrip.Player.Id);
        Assert.Equal(state.Player.Attributes, roundTrip.Player.Attributes);
        Assert.Equal(state.Player.Position, roundTrip.Player.Position);
        Assert.Equal(state.Player.Inventory, roundTrip.Player.Inventory);
        Assert.Equal(state.Player.CarriedGold, roundTrip.Player.CarriedGold);
        Assert.Equal(state.Expedition.Active, roundTrip.Expedition.Active);
        Assert.Equal(state.Expedition.CarriedGold, roundTrip.Expedition.CarriedGold);
        Assert.Equal(state.Expedition.FloorsVisited, roundTrip.Expedition.FloorsVisited);
        Assert.Equal(state.Inn, roundTrip.Inn);
        Assert.Equal(state.SecuredProgress, roundTrip.SecuredProgress);
        Assert.Equal(state.Legacy.PersistentMap.ObservedPositions, roundTrip.Legacy.PersistentMap.ObservedPositions);
        Assert.Equal(state.Legacy.PersistentMap.VisitedPositions, roundTrip.Legacy.PersistentMap.VisitedPositions);
        Assert.Equal(state.Legacy.PreviousHeroes, roundTrip.Legacy.PreviousHeroes);
        Assert.Equal(state.Legacy.Graves, roundTrip.Legacy.Graves);
        Assert.Equal(state.Legacy.Heirlooms, roundTrip.Legacy.Heirlooms);
    }

    [Fact]
    public void Deserialize_rejects_unsupported_save_versions()
    {
        var json = SaveGameSerializer.Serialize(GameState.Create(1234)).Replace("\"saveVersion\": 13", "\"saveVersion\": 14");

        Assert.Throws<SaveFormatException>(() => SaveGameSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_migrates_version_one_saves_with_an_empty_persistent_map()
    {
        var json = SaveGameSerializer.Serialize(GameState.Create(1234)).Replace("\"saveVersion\": 13", "\"saveVersion\": 1");

        var state = SaveGameSerializer.Deserialize(json);

        Assert.Equal(GameState.CurrentSaveVersion, state.SaveVersion);
        Assert.Empty(state.Legacy.PersistentMap.ObservedPositions);
        Assert.Empty(state.Legacy.PersistentMap.VisitedPositions);
    }

    [Fact]
    public void Deserialize_migrates_version_ten_saves_without_dead_hero_records()
    {
        var document = JsonNode.Parse(SaveGameSerializer.Serialize(GameState.Create(1234)))!.AsObject();
        document["saveVersion"] = 10;
        document["legacy"]!.AsObject().Remove("previousHeroes");

        var state = SaveGameSerializer.Deserialize(document.ToJsonString());

        Assert.Equal(GameState.CurrentSaveVersion, state.SaveVersion);
        Assert.Empty(state.Legacy.PreviousHeroes);
    }

    [Fact]
    public void Deserialize_migrates_version_eleven_saves_without_graves()
    {
        var document = JsonNode.Parse(SaveGameSerializer.Serialize(GameState.Create(1234)))!.AsObject();
        document["saveVersion"] = 11;
        document["legacy"]!.AsObject().Remove("graves");

        var state = SaveGameSerializer.Deserialize(document.ToJsonString());

        Assert.Equal(GameState.CurrentSaveVersion, state.SaveVersion);
        Assert.Empty(state.Legacy.Graves);
    }

    [Fact]
    public void Deserialize_migrates_version_twelve_saves_without_heirlooms()
    {
        var document = JsonNode.Parse(SaveGameSerializer.Serialize(GameState.Create(1234)))!.AsObject();
        document["saveVersion"] = 12;
        document["legacy"]!.AsObject().Remove("heirlooms");

        var state = SaveGameSerializer.Deserialize(document.ToJsonString());

        Assert.Equal(GameState.CurrentSaveVersion, state.SaveVersion);
        Assert.Empty(state.Legacy.Heirlooms);
    }

    [Fact]
    public void Deserialize_migrates_version_two_saves_to_the_inn()
    {
        var document = JsonNode.Parse(SaveGameSerializer.Serialize(GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false }
        }))!.AsObject();
        document["saveVersion"] = 2;
        document.Remove("inn");

        var state = SaveGameSerializer.Deserialize(document.ToJsonString());

        Assert.Equal(GameState.CurrentSaveVersion, state.SaveVersion);
        Assert.True(state.Inn.IsAtInn);
    }

    [Fact]
    public void Deserialize_migrates_version_three_saves_with_zero_secured_gold()
    {
        var document = JsonNode.Parse(SaveGameSerializer.Serialize(GameState.Create(1234) with
        {
            SecuredProgress = new SecuredProgressState { SecuredGold = 23 }
        }))!.AsObject();
        document["saveVersion"] = 3;
        document["securedProgress"] = new JsonObject();

        var state = SaveGameSerializer.Deserialize(document.ToJsonString());

        Assert.Equal(GameState.CurrentSaveVersion, state.SaveVersion);
        Assert.Equal(0, state.SecuredProgress.SecuredGold);
    }

    [Fact]
    public void Deserialize_migrates_version_five_combat_without_a_threat_level()
    {
        var state = GameState.Create(1234) with
        {
            Expedition = new ExpeditionState { Active = true },
            Player = new PlayerState { Position = new DungeonPosition(1, 0, 0) },
            Combat = new CombatState(
                new MonsterInstance(
                    Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    "rat",
                    1,
                    3,
                    new DungeonPosition(1, 0, 0)),
                CombatPhase.PlayerAction,
                threatLevel: ThreatLevel.Dangerous)
        };
        var document = JsonNode.Parse(SaveGameSerializer.Serialize(state))!.AsObject();
        document["saveVersion"] = 5;
        document["combat"]!["threatLevel"] = null;

        var migrated = SaveGameSerializer.Deserialize(document.ToJsonString());

        Assert.Equal(GameState.CurrentSaveVersion, migrated.SaveVersion);
        Assert.Equal(CombatPhase.PlayerAction, migrated.Combat!.Phase);
        Assert.Null(migrated.Combat.ThreatLevel);
    }

    [Fact]
    public void Deserialize_migrates_version_one_expedition_fields_with_derived_floor_defaults()
    {
        var document = JsonNode.Parse(SaveGameSerializer.Serialize(GameState.Create(1234) with
        {
            Expedition = new ExpeditionState
            {
                Active = true,
                CarriedGold = 17,
                FloorsVisited = [1, 7]
            }
        }))!.AsObject();
        document["saveVersion"] = 1;
        var expedition = document["expedition"]!.AsObject();
        foreach (var property in new[]
        {
            "expeditionId", "startingFloor", "deepestFloorReached", "startSimulationTick",
            "simulationTicks", "acquiredItems", "monstersDefeated", "discoveriesMade",
            "roomsVisited", "objectives"
        })
        {
            expedition.Remove(property);
        }

        var state = SaveGameSerializer.Deserialize(document.ToJsonString());

        Assert.True(state.Expedition.Active);
        Assert.Equal(17, state.Expedition.CarriedGold);
        Assert.Equal(1, state.Expedition.StartingFloor);
        Assert.Equal(7, state.Expedition.DeepestFloorReached);
        Assert.Equal([1, 7], state.Expedition.FloorsVisited);
        Assert.Empty(state.Expedition.AcquiredItems);
        Assert.Empty(state.Expedition.DiscoveriesMade);
        Assert.Empty(state.Expedition.Objectives);
    }

    [Fact]
    public void Version_one_migration_preserves_explicit_expedition_floor_values()
    {
        var save = GameStateSaveDto.FromState(GameState.Create(1234)) with
        {
            SaveVersion = 1,
            Expedition = new ExpeditionStateDto
            {
                StartingFloor = 4,
                DeepestFloorReached = 9,
                FloorsVisited = [2, 6],
                AcquiredItems = [],
                DiscoveriesMade = [],
                Objectives = []
            }
        };

        var migrated = SaveMigrations.Migrate(save);

        Assert.Equal(4, migrated.Expedition.StartingFloor);
        Assert.Equal(9, migrated.Expedition.DeepestFloorReached);
    }

    [Fact]
    public void Version_one_migration_defaults_missing_floor_history_to_floor_one()
    {
        var save = GameStateSaveDto.FromState(GameState.Create(1234)) with
        {
            SaveVersion = 1,
            Expedition = new ExpeditionStateDto
            {
                StartingFloor = 0,
                DeepestFloorReached = 0,
                FloorsVisited = [],
                AcquiredItems = [],
                DiscoveriesMade = [],
                Objectives = []
            }
        };

        var migrated = SaveMigrations.Migrate(save);

        Assert.Equal(1, migrated.Expedition.StartingFloor);
        Assert.Equal(1, migrated.Expedition.DeepestFloorReached);
    }

    [Fact]
    public void Version_four_migration_advances_to_the_current_schema_and_clears_combat()
    {
        var state = GameState.Create(1234) with
        {
            Combat = new CombatState(
                new MonsterInstance(
                    Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    "rat",
                    1,
                    3,
                    new DungeonPosition(1, 0, 0)))
        };
        var save = GameStateSaveDto.FromState(state) with { SaveVersion = 4 };

        var migrated = SaveMigrations.Migrate(save);

        Assert.Equal(GameState.CurrentSaveVersion, migrated.SaveVersion);
        Assert.Null(migrated.Combat);
    }

    [Fact]
    public void Deserialize_rejects_visited_positions_that_are_not_observed()
    {
        var json = SaveGameSerializer.Serialize(GameState.Create(1234)).Replace(
            "\"visitedPositions\": []",
            "\"visitedPositions\": [{\"floor\":1,\"x\":2,\"y\":3}]");

        Assert.Throws<SaveFormatException>(() => SaveGameSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_rejects_invalid_json()
    {
        Assert.Throws<SaveFormatException>(() => SaveGameSerializer.Deserialize("not-json"));
    }

    [Fact]
    public void Save_round_trip_is_deterministic()
    {
        var state = GameState.Create(1234, playerId: Guid.Parse("00000000-0000-0000-0000-000000000001"));

        Assert.Equal(SaveGameSerializer.Serialize(state), SaveGameSerializer.Serialize(SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state))));
    }

    [Fact]
    public void Validation_rejects_each_missing_required_state_boundary()
    {
        var save = GameStateSaveDto.FromState(GameState.Create(1234));

        foreach (var invalid in new[]
        {
            save with { Versions = null! },
            save with { Player = null! },
            save with { Expedition = null! },
            save with { Dungeon = null! },
            save with { Knowledge = null! },
            save with { Legacy = null! },
            save with { Inn = null },
            save with { SecuredProgress = null! },
            save with { Settings = null! },
            save with { CurrentMode = (GameMode)999 }
        })
        {
            Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(invalid));
        }

        foreach (var invalid in new[]
        {
            save with { Player = save.Player with { Attributes = null! } },
            save with { Player = save.Player with { Position = null! } },
            save with { Player = save.Player with { Inventory = null! } },
            save with { Player = save.Player with { EquipmentSlots = null! } },
            save with { Player = save.Player with { Talents = null! } },
            save with { Player = save.Player with { Spells = null! } },
            save with { Player = save.Player with { Injuries = null! } },
            save with { Player = save.Player with { TemporaryEffects = null! } },
            save with { Expedition = save.Expedition with { AcquiredItems = null } },
            save with { Expedition = save.Expedition with { DiscoveriesMade = null } },
            save with { Expedition = save.Expedition with { FloorsVisited = null } }
        })
        {
            Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(invalid));
        }

        Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(save with
        {
            Legacy = save.Legacy with { PersistentMap = null! }
        }));
        Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(save with
        {
            Legacy = save.Legacy with { PreviousHeroes = null }
        }));
        Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(save with
        {
            Legacy = save.Legacy with { Graves = null }
        }));
        Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(save with
        {
            Legacy = save.Legacy with { Heirlooms = null }
        }));
        Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(save with
        {
            Legacy = save.Legacy with
            {
                Heirlooms = [new HeirloomRecordDto { HeroId = Guid.Empty, ItemId = "item" }]
            }
        }));
        Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(save with
        {
            Legacy = save.Legacy with
            {
                Heirlooms = [new HeirloomRecordDto { HeroId = Guid.NewGuid(), ItemId = " " }]
            }
        }));
        Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(save with
        {
            Legacy = save.Legacy with
            {
                PersistentMap = save.Legacy.PersistentMap with { ObservedPositions = null! }
            }
        }));
        Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(save with
        {
            Legacy = save.Legacy with
            {
                PersistentMap = save.Legacy.PersistentMap with { VisitedPositions = null! }
            }
        }));
    }

    [Fact]
    public void Validation_rejects_each_invalid_combat_boundary()
    {
        var state = GameState.Create(1234) with
        {
            Combat = new CombatState(
                new MonsterInstance(
                    Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    "rat",
                    1,
                    3,
                    new DungeonPosition(1, 0, 0)),
                CombatPhase.PlayerAction,
                threatLevel: ThreatLevel.Dangerous)
        };
        var save = GameStateSaveDto.FromState(state);

        foreach (var invalid in new[]
        {
            save with { Combat = save.Combat! with { Monster = null! } },
            save with { Combat = save.Combat! with { Monster = save.Combat.Monster with { Position = null! } } },
            save with { Combat = save.Combat! with { Monster = save.Combat.Monster with { TemporaryEffects = null! } } },
            save with { Combat = save.Combat! with { Phase = (CombatPhase)999 } },
            save with { Combat = save.Combat! with { SelectedAction = (CombatAction)999 } },
            save with { Combat = save.Combat! with { ThreatLevel = (ThreatLevel)999 } }
        })
        {
            Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(invalid));
        }
    }

    [Fact]
    public void Serializer_wraps_invalid_state_and_null_inputs_at_the_boundary()
    {
        var document = JsonNode.Parse(SaveGameSerializer.Serialize(GameState.Create(1234)))!.AsObject();
        document["player"]!["position"]!["floor"] = 51;

        Assert.Throws<SaveFormatException>(() => SaveGameSerializer.Deserialize(document.ToJsonString()));
        Assert.Throws<SaveFormatException>(() => SaveGameSerializer.Deserialize("null"));
        Assert.Throws<ArgumentNullException>(() => SaveGameSerializer.Serialize(null!));
        Assert.Throws<ArgumentException>(() => SaveGameSerializer.Deserialize(" "));
        Assert.Throws<ArgumentNullException>(() => SaveMigrations.Migrate(null!));
        Assert.Throws<ArgumentNullException>(() => SaveMigrations.Validate(null!));

        Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(
            GameStateSaveDto.FromState(GameState.Create(1234)) with { SaveVersion = 14 }));
    }

    [Fact]
    public void Expedition_dto_defaults_missing_collections()
    {
        var expedition = new ExpeditionStateDto
        {
            AcquiredItems = null,
            DiscoveriesMade = null,
            FloorsVisited = null,
            Objectives = null
        };

        var state = expedition.ToState();

        Assert.Empty(state.AcquiredItems);
        Assert.Empty(state.DiscoveriesMade);
        Assert.Empty(state.FloorsVisited);
        Assert.Empty(state.Objectives);

        var versionOne = new ExpeditionStateDto
        {
            FloorsVisited = null,
            StartingFloor = 1,
            DeepestFloorReached = 1,
            AcquiredItems = [],
            DiscoveriesMade = [],
            Objectives = []
        };
        Assert.Empty(SaveMigrations.Migrate(GameStateSaveDto.FromState(GameState.Create(1234)) with
        {
            SaveVersion = 1,
            Expedition = versionOne
        }).Expedition.FloorsVisited!);
    }

    [Fact]
    public void Version_one_migration_materializes_missing_expedition_collections()
    {
        var save = GameStateSaveDto.FromState(GameState.Create(1234)) with
        {
            SaveVersion = 1,
            Expedition = new ExpeditionStateDto
            {
                StartingFloor = 1,
                DeepestFloorReached = 1,
                FloorsVisited = [],
                AcquiredItems = ["relic"],
                DiscoveriesMade = ["shrine"],
                Objectives = ["escape"]
            }
        };

        var migrated = SaveMigrations.Migrate(save).Expedition;

        Assert.Equal(["relic"], migrated.AcquiredItems);
        Assert.Equal(["shrine"], migrated.DiscoveriesMade);
        Assert.Equal(["escape"], migrated.Objectives);

        var missing = save with
        {
            Expedition = save.Expedition with
            {
                AcquiredItems = null,
                DiscoveriesMade = null,
                Objectives = null
            }
        };
        var migratedMissing = SaveMigrations.Migrate(missing).Expedition;

        Assert.Empty(migratedMissing.AcquiredItems!);
        Assert.Empty(migratedMissing.DiscoveriesMade!);
        Assert.Empty(migratedMissing.Objectives!);
    }
}
