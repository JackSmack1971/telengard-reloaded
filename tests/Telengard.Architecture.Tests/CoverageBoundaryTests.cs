using System.Collections;
using System.Text.Json;
using Telengard.Content;
using Telengard.Core.Items;
using Telengard.Core.Knowledge;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Save;
using Telengard.Save.Dto;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class CoverageBoundaryTests
{
    private static readonly Guid FeatureId = Guid.Parse("00000000-0000-0000-0000-000000000061");
    private static readonly Guid ItemId = Guid.Parse("00000000-0000-0000-0000-000000000062");

    [Fact]
    public void Item_and_result_boundaries_preserve_validation_and_empty_event_contracts()
    {
        var item = new ItemInstance(ItemId, "sword");
        var player = new PlayerState();

        Assert.Throws<ArgumentOutOfRangeException>(() => new ItemObservedState(ItemId, false, durability: -1));
        Assert.Throws<ArgumentException>(() => new ItemObservedState(Guid.Empty, false));
        Assert.Throws<ArgumentException>(() => new ItemInstance(ItemId, "sword").WithGeneratedAffixes(["keen", "keen"]));
        Assert.Empty(new GenerateItemAffixesCommand(ItemId).Affixes);
        Assert.Throws<ArgumentException>(() => new EquipmentSlotState("weapon").Equip(Guid.Empty));

        Assert.Empty(new EquipmentResult(player).Events);
        Assert.Throws<ArgumentNullException>(() => new EquipmentResult(null!));
        Assert.Empty(new ItemAffixGenerationResult(item).Events);
        Assert.Throws<ArgumentNullException>(() => new ItemAffixGenerationResult(null!));
        Assert.Empty(new ItemCurseResult(item).Events);
        Assert.Throws<ArgumentNullException>(() => new ItemCurseResult(null!));
        Assert.Empty(new ItemIdentificationResult(item).Events);
        Assert.Throws<ArgumentNullException>(() => new ItemIdentificationResult(null!));
        Assert.Throws<ArgumentNullException>(() => new EquipmentSlotState(null!));
        Assert.Throws<ArgumentException>(() => new EquipmentSlotState(""));
        var identified = item.Identify();
        Assert.Same(identified, identified.Identify());
    }

    [Fact]
    public void Equipment_resolvers_cover_dead_player_and_already_equipped_boundaries()
    {
        var slots = new[]
        {
            new EquipmentSlotState("weapon", ItemId),
            new EquipmentSlotState("off-hand")
        };
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState { EquipmentSlots = slots },
            Expedition = new ExpeditionState { Active = true }
        };

        Assert.Throws<InvalidOperationException>(() => EquipmentResolver.Unequip(
            state with { Player = state.Player with { Alive = false } },
            new UnequipItemCommand("weapon")));
        Assert.Throws<InvalidOperationException>(() => EquipmentResolver.Equip(
            state,
            new EquipItemCommand("off-hand", ItemId)));
        var laterMatch = state with
        {
            Player = state.Player with
            {
                EquipmentSlots = [
                    new EquipmentSlotState("weapon"),
                    new EquipmentSlotState("off-hand", ItemId)]
            }
        };
        Assert.Throws<InvalidOperationException>(() => EquipmentResolver.Equip(
            laterMatch,
            new EquipItemCommand("weapon", ItemId)));
        Assert.Equal(ItemId, state.Player.EquipmentSlots[0].ItemInstanceId);
    }

    [Fact]
    public void State_collections_reject_null_entries_and_duplicate_authoritative_ids()
    {
        Assert.Throws<ArgumentException>(() => new PlayerState
        {
            EquipmentSlots = new[] { (EquipmentSlotState)null! }
        });

        var feature = Feature();
        Assert.Throws<ArgumentException>(() => new DungeonState
        {
            Features = new[] { (FeatureInstance)null! }
        });
        Assert.Throws<ArgumentException>(() => new DungeonState
        {
            Features = [feature, feature]
        });

        var entry = new KnowledgeEntry("subject");
        Assert.Throws<ArgumentException>(() => new KnowledgeState(
            new[] { (KnowledgeEntry)null! }));
        Assert.Throws<ArgumentException>(() => new KnowledgeState([entry, entry]));

        var mapping = Mapping(TeleporterMappingStatus.Observed);
        Assert.Throws<ArgumentException>(() => new KnowledgeState(
            teleporterMappings: new[] { (TeleporterMapping)null! }));
        Assert.Throws<ArgumentException>(() => new KnowledgeState(
            teleporterMappings: [mapping, mapping]));
    }

    [Fact]
    public void Feature_schema_and_activation_cover_duplicate_tags_and_variant_effects()
    {
        Assert.Throws<ArgumentException>(() => new FeatureOutcome(["requires:key", "requires:key"]));
        Assert.Throws<ArgumentException>(() => new FeatureOutcomeResolution(["effect", "effect"]));
        Assert.Throws<ArgumentException>(() => new FeatureOutcomeResolution(observations: ["observation", "observation"]));
        Assert.Throws<ArgumentException>(() => new FeatureDefinition(
            "fountain", FeatureType.Fountain, "fountain", ["drink", "drink"]));
        Assert.Throws<ArgumentNullException>(() => new FeatureDefinition(
            "fountain", FeatureType.Fountain, "fountain",
            outcomeTable: new FeatureOutcome[] { null! }));
        Assert.Throws<ArgumentException>(() => new FeatureDefinition(
            "fountain", FeatureType.Fountain, "fountain",
            hintRules: new DuplicateRuleDictionary()));

        var resolution = new FeatureOutcomeResolution(["effect"], ["observation"]);
        var state = ActiveState(Feature()) with
        {
            Dungeon = new DungeonState
            {
                Features = [Feature(),
                    new FeatureInstance(
                Guid.Parse("00000000-0000-0000-0000-000000000063"), "other", new DungeonPosition(1, 2, 2))]
            },
            Player = new PlayerState { Position = new DungeonPosition(1, 0, 0) },
            Expedition = new ExpeditionState
            {
                Active = true,
                FloorsVisited = [1, 3],
                ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000064")
            }
        };

        var generic = FeatureActivationResolver.Activate(
            state,
            new ActivateFeatureCommand(FeatureId),
            resolution);
        Assert.Equal(2, generic.State.Dungeon.Features.Count);
        var genericEvent = Assert.IsType<FeatureOutcomeResolvedEvent>(generic.Events[^1]);
        Assert.Equal(FeatureId, genericEvent.FeatureId);
        Assert.Equal(new DungeonPosition(1, 0, 0), genericEvent.Position);
        Assert.Equal(1, genericEvent.ActivationCount);
        var discovered = Assert.IsType<FeatureDiscoveredEvent>(generic.Events[0]);
        Assert.Equal(FeatureId, discovered.FeatureId);
        Assert.Equal(new DungeonPosition(1, 0, 0), discovered.Position);
        var activated = Assert.IsType<FeatureActivatedEvent>(generic.Events[1]);
        Assert.Equal(FeatureId, activated.FeatureId);
        Assert.Equal(new DungeonPosition(1, 0, 0), activated.Position);
        Assert.Equal(1, activated.ActivationCount);

        var pit = FeatureActivationResolver.ActivatePit(
            ActiveState(Feature()) with
            {
                Player = new PlayerState { Position = new DungeonPosition(1, 0, 0) },
                Expedition = new ExpeditionState { Active = true, FloorsVisited = [1, 3] }
            },
            new ActivateFeatureCommand(FeatureId),
            new FeatureOutcomeResolution([PitEffectIds.DropTwoFloors]));
        Assert.Equal(3, pit.State.Player.Position.Floor);
        Assert.Equal([1, 3], pit.State.Expedition.FloorsVisited);

        var teleporter = FeatureActivationResolver.ActivateTeleporter(
            ActiveState(Feature()) with
            {
                Player = new PlayerState { Position = new DungeonPosition(1, 0, 0) },
                Expedition = new ExpeditionState { Active = true, FloorsVisited = [1, 3] }
            },
            new ActivateFeatureCommand(FeatureId),
            new DungeonPosition(3, 5, 5),
            new FeatureOutcomeResolution(["configured_route"]));
        Assert.Equal(new DungeonPosition(3, 5, 5), teleporter.State.Player.Position);
        Assert.Contains(3, teleporter.State.Expedition.FloorsVisited);

        Assert.Throws<OverflowException>(() => new FeatureInstance(
            FeatureId, "feature", new DungeonPosition(1, 0, 0), int.MaxValue).Activate());
        Assert.Throws<ArgumentException>(() => new FeatureInstance(
            Guid.Empty, "feature", new DungeonPosition(1, 0, 0)));
        Assert.Throws<ArgumentException>(() => new TeleporterMapping(
            "network", "node", new DungeonPosition(1, 0, 0), new DungeonPosition(1, 1, 1),
            TeleporterMappingStatus.Unknown));
    }

    [Fact]
    public void Knowledge_and_mapping_boundaries_cover_zero_progression_and_idempotence()
    {
        var configuration = new KnowledgeConfidenceConfiguration();
        Assert.Equal(0, configuration.Resolve(0));
        Assert.Throws<ArgumentException>(() => new KnowledgeConfidenceConfiguration(probableSampleCount: 2));
        Assert.Throws<ArgumentException>(() => new KnowledgeConfidenceConfiguration(highConfidenceSampleCount: 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => new KnowledgeConfidenceConfiguration(suspectedConfidence: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new KnowledgeConfidenceConfiguration(probableConfidence: 101));
        Assert.Throws<ArgumentOutOfRangeException>(() => new KnowledgeConfidenceConfiguration(highConfidence: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new KnowledgeConfidenceConfiguration(highConfidence: 101));
        Assert.Throws<ArgumentException>(() => new KnowledgeConfidenceConfiguration(
            suspectedConfidence: 10, rumorConfidence: 20));

        var firstEntry = new KnowledgeEntry("first");
        var state = ActiveState(Feature()) with
        {
            Knowledge = new KnowledgeState([firstEntry, new KnowledgeEntry("second")])
        };
        var added = KnowledgeObservationResolver.Add(
            state,
            new AddKnowledgeObservationCommand("first", ["new"]));
        Assert.Equal(2, added.State.Knowledge.Entries.Count);

        var existing = Mapping(TeleporterMappingStatus.Mapped);
        var other = new TeleporterMapping(
            "network", "other", new DungeonPosition(1, 0, 0), new DungeonPosition(1, 2, 2));
        var mappingState = ActiveState(Feature()) with
        {
            Knowledge = new KnowledgeState(teleporterMappings: [existing, other])
        };
        var command = new AddTeleporterMappingCommand(
            existing.NetworkId, existing.NodeId, existing.Source, existing.Destination);
        var unchanged = TeleporterMappingResolver.Add(mappingState, command);
        Assert.Same(mappingState, unchanged.State);
        Assert.Empty(unchanged.Events);

        var observedState = mappingState with
        {
            Knowledge = new KnowledgeState(teleporterMappings: [
                Mapping(TeleporterMappingStatus.Observed),
                other])
        };
        var observedCommand = new AddTeleporterMappingCommand(
            "network", "node", new DungeonPosition(1, 0, 0), new DungeonPosition(2, 1, 1));
        var confirmed = TeleporterMappingResolver.Add(observedState, observedCommand);
        Assert.Equal(TeleporterMappingStatus.Mapped, Assert.Single(
            confirmed.State.Knowledge.TeleporterMappings,
            item => item.NodeId == "node").Status);

        var observed = new TeleporterMappingObservedEvent(
            existing.NetworkId, existing.NodeId, existing.Source, existing.Destination, existing.Status);
        Assert.Equal(existing.NetworkId, observed.NetworkId);
        Assert.Equal(existing.NodeId, observed.NodeId);
        Assert.Equal(existing.Source, observed.Source);
        Assert.Equal(existing.Destination, observed.Destination);
        Assert.Equal(existing.Status, observed.Status);
        Assert.Equal(TeleporterMappingStatus.Unknown, TeleporterMappingResolver.GetStatus(
            mappingState.Knowledge, "other-network", "other-node", existing.Source, existing.Destination));
    }

    [Fact]
    public void Content_engines_cover_stream_overloads_and_teleporter_node_boundaries()
    {
        var item = new ItemInstance(ItemId, "sword");
        var definition = new ItemDefinition(
            "sword", "Sword", "weapon", affixPool: ["keen"], cursePool: ["brittle"]);
        var rng = new Telengard.Core.Rng.DeterministicRng(42, "content").CreateStream("test");

        var affixed = ItemAffixEngine.Generate(item, definition, 1, rng);
        Assert.Equal(["keen"], affixed.Item.GeneratedAffixes);
        var cursed = ItemCurseEngine.Generate(item, definition, rng);
        Assert.Equal("brittle", cursed.Item.Curse);
        Assert.Throws<InvalidOperationException>(() => ItemAffixEngine.Generate(
            item,
            new ItemDefinition("other", "Other", "weapon", affixPool: ["keen"]),
            1,
            new Telengard.Core.Rng.DeterministicRng(42, "content").CreateStream("other")));

        var outcomes = new FeatureDefinition(
            "feature",
            FeatureType.Fountain,
            "fountain",
            outcomeTable: [
                new FeatureOutcome(weight: 1, effects: ["first"]),
                new FeatureOutcome(weight: 1, effects: ["second"])]);
        var secondOutcome = Enumerable.Range(0, 100)
            .Select(seed => FeatureOutcomeEngine.Select(
                outcomes,
                new FeatureOutcomeSelectionContext(),
                seed,
                "content",
                FeatureId,
                new DungeonPosition(1, 0, 0),
                0))
            .First(outcome => outcome.Effects.Contains("second"));
        Assert.Equal(["second"], secondOutcome.Effects);
        Assert.Throws<InvalidOperationException>(() => ItemCurseEngine.Generate(
            item,
            new ItemDefinition("other", "Other", "weapon", cursePool: ["brittle"]),
            new Telengard.Core.Rng.DeterministicRng(42, "content").CreateStream("other")));

        var feature = Feature();
        var teleporterDefinition = new FeatureDefinition(
            "feature", FeatureType.Teleporter, "teleporter", outcomeTable: [new FeatureOutcome(weight: 1)]);
        var node = new TeleporterNode("node", feature.Position, "network", "rule");
        Assert.Equal("rule", node.DestinationRule);
        Assert.Throws<InvalidOperationException>(() => TeleporterResolver.Resolve(
            ActiveState(feature),
            new ActivateFeatureCommand(feature.InstanceId),
            teleporterDefinition,
            new TeleporterNode("node", new DungeonPosition(1, 1, 1), "network", "rule"),
            new DungeonPosition(2, 2, 2)));
        Assert.Throws<InvalidOperationException>(() => TeleporterResolver.Resolve(
            ActiveState(feature),
            new ActivateFeatureCommand(Guid.NewGuid()),
            teleporterDefinition,
            new DungeonPosition(2, 2, 2)));

        Assert.Throws<InvalidOperationException>(() => AltarResolver.Resolve(
            ActiveState(feature),
            new ActivateFeatureCommand(Guid.NewGuid()),
            new FeatureDefinition("feature", FeatureType.Altar, "altar", outcomeTable: [new FeatureOutcome(weight: 1)])));
        Assert.Throws<InvalidOperationException>(() => FountainResolver.Resolve(
            ActiveState(feature),
            new ActivateFeatureCommand(Guid.NewGuid()),
            new FeatureDefinition("feature", FeatureType.Fountain, "fountain", outcomeTable: [new FeatureOutcome(weight: 1)])));
        Assert.Throws<InvalidOperationException>(() => PitResolver.Resolve(
            ActiveState(feature),
            new ActivateFeatureCommand(Guid.NewGuid()),
            new FeatureDefinition("feature", FeatureType.Pit, "pit", outcomeTable: [new FeatureOutcome(weight: 1)])));
    }

    [Fact]
    public void Save_dtos_materialize_nullable_collections_and_converter_boundaries()
    {
        Assert.Empty(new DungeonStateDto { Features = null }.ToState().Features);
        var knowledge = new KnowledgeStateDto { Entries = null, TeleporterMappings = null }.ToState();
        Assert.Empty(knowledge.Entries);
        Assert.Empty(knowledge.TeleporterMappings);
        var entry = new KnowledgeEntryDto
        {
            SubjectId = "subject",
            Observations = null,
            Hypotheses = null,
            ConfirmedFacts = null
        }.ToState();
        Assert.Empty(entry.Observations);
        Assert.Empty(entry.Hypotheses);
        Assert.Empty(entry.ConfirmedFacts);
        Assert.Empty(new ItemInstanceDto
        {
            InstanceId = ItemId,
            DefinitionId = "sword",
            GeneratedAffixes = null
        }.ToState().GeneratedAffixes);

        var id = Guid.Parse("00000000-0000-0000-0000-000000000065");
        var upper = JsonSerializer.Deserialize<EquipmentSlotDto>(
            $"{{\"SlotId\":\"weapon\",\"ItemInstanceId\":\"{id}\"}}")!;
        Assert.Equal("weapon", upper.SlotId);
        Assert.Equal(id, upper.ItemInstanceId);
        foreach (var json in new[]
        {
            "1",
            "{}",
            "{\"slotId\":\" \"}",
            "{\"slotId\":\"weapon\",\"itemInstanceId\":42}"
        })
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EquipmentSlotDto>(json));
        }
    }

    [Fact]
    public void Legacy_save_dto_materializes_missing_history_collections()
    {
        var legacy = new LegacyStateDto
        {
            PersistentMap = PersistentMapStateDto.FromState(new PersistentMapState()),
            PreviousHeroes = null,
            Graves = null,
            Heirlooms = null
        }.ToState();

        Assert.Empty(legacy.PreviousHeroes);
        Assert.Empty(legacy.Graves);
        Assert.Empty(legacy.Heirlooms);
    }

    [Fact]
    public void Save_migrations_cover_nullable_version_boundaries_and_invalid_feature_collections()
    {
        var baseSave = GameStateSaveDto.FromState(GameState.Create(1234));
        var versionSeven = SaveMigrations.Migrate(baseSave with { SaveVersion = 7, Dungeon = null! });
        Assert.Empty(versionSeven.Dungeon.Features!);
        var versionEight = SaveMigrations.Migrate(baseSave with { SaveVersion = 8, Knowledge = null! });
        Assert.Empty(versionEight.Knowledge.Entries!);
        var versionNine = SaveMigrations.Migrate(baseSave with { SaveVersion = 9, Knowledge = null! });
        Assert.Empty(versionNine.Knowledge.TeleporterMappings!);

        var feature = FeatureDto();
        var invalidFeatures = new[]
        {
            baseSave with { Dungeon = baseSave.Dungeon with { Features = null } },
            baseSave with { Dungeon = baseSave.Dungeon with { Features = [(FeatureInstanceDto)null!] } },
            baseSave with { Dungeon = baseSave.Dungeon with { Features = [feature with { InstanceId = Guid.Empty }] } },
            baseSave with { Dungeon = baseSave.Dungeon with { Features = [feature with { DefinitionId = " " }] } },
            baseSave with { Dungeon = baseSave.Dungeon with { Features = [feature with { Position = null! }] } },
            baseSave with { Dungeon = baseSave.Dungeon with { Features = [feature with { ActivationCount = -1 }] } },
            baseSave with { Dungeon = baseSave.Dungeon with { Features = [feature, feature] } }
        };
        foreach (var invalid in invalidFeatures)
        {
            Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(invalid));
        }

        var slot = new EquipmentSlotDto { SlotId = "weapon", ItemInstanceId = ItemId };
        var invalidSlots = new[]
        {
            baseSave with { Player = baseSave.Player with { EquipmentSlots = new EquipmentSlotDto[] { null! } } },
            baseSave with { Player = baseSave.Player with { EquipmentSlots = [slot with { SlotId = null! }] } },
            baseSave with { Player = baseSave.Player with { EquipmentSlots = [slot with { SlotId = " " }] } },
            baseSave with { Player = baseSave.Player with { EquipmentSlots = [slot with { ItemInstanceId = Guid.Empty }] } },
            baseSave with { Player = baseSave.Player with { EquipmentSlots = [slot, slot with { SlotId = "weapon" }] } },
            baseSave with { Player = baseSave.Player with { EquipmentSlots = [slot, new EquipmentSlotDto { SlotId = "off-hand", ItemInstanceId = ItemId }] } }
        };
        foreach (var invalid in invalidSlots)
        {
            Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(invalid));
        }
        SaveMigrations.Validate(baseSave with
        {
            Player = baseSave.Player with
            {
                EquipmentSlots = [new EquipmentSlotDto { SlotId = "weapon", ItemInstanceId = null }]
            }
        });

        var entry = EntryDto();
        var invalidEntries = new[]
        {
            baseSave with { Knowledge = baseSave.Knowledge with { Entries = new KnowledgeEntryDto[] { null! } } },
            baseSave with { Knowledge = baseSave.Knowledge with { Entries = [entry with { SubjectId = " " }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { Entries = [entry with { SampleCount = -1 }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { Entries = [entry with { Confidence = -1 }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { Entries = [entry with { Confidence = 101 }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { Entries = [entry with { Observations = null }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { Entries = [entry with { Observations = [" "] }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { Entries = [entry with { Observations = ["same", "same"] }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { Entries = [entry with { Hypotheses = null }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { Entries = [entry with { Hypotheses = [" "] }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { Entries = [entry with { ConfirmedFacts = null }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { Entries = [entry with { ConfirmedFacts = [" "] }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { Entries = [entry, entry with { SubjectId = "subject" }] } }
        };
        for (var index = 0; index < invalidEntries.Length; index++)
        {
            var invalid = invalidEntries[index];
            var thrown = false;
            try
            {
                SaveMigrations.Validate(invalid);
            }
            catch (SaveFormatException)
            {
                thrown = true;
            }

            Assert.True(thrown, $"Invalid knowledge entry case {index} was accepted.");
        }

        var mapping = MappingDto();
        var invalidMappings = new[]
        {
            baseSave with { Knowledge = baseSave.Knowledge with { TeleporterMappings = null } },
            baseSave with { Knowledge = baseSave.Knowledge with { TeleporterMappings = new TeleporterMappingDto[] { null! } } },
            baseSave with { Knowledge = baseSave.Knowledge with { TeleporterMappings = [mapping with { NetworkId = " " }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { TeleporterMappings = [mapping with { NodeId = " " }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { TeleporterMappings = [mapping with { Source = null! }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { TeleporterMappings = [mapping with { Destination = null! }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { TeleporterMappings = [mapping with { Status = (TeleporterMappingStatus)999 }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { TeleporterMappings = [mapping with { Status = TeleporterMappingStatus.Unknown }] } },
            baseSave with { Knowledge = baseSave.Knowledge with { TeleporterMappings = [mapping, mapping] } }
        };
        foreach (var invalid in invalidMappings)
        {
            Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(invalid));
        }
    }

    [Fact]
    public void Save_validation_rejects_invalid_floors_in_each_persisted_position_collection()
    {
        var baseSave = GameStateSaveDto.FromState(GameState.Create(1234));
        var position = new DungeonPositionDto { Floor = 0, X = 1, Y = 1 };
        var upperPosition = new DungeonPositionDto { Floor = 51, X = 1, Y = 1 };
        var mapping = MappingDto();
        var feature = FeatureDto();
        var hero = new DeadHeroRecordDto
        {
            HeroId = FeatureId,
            Attributes = new PlayerAttributesDto(),
            DeathPosition = position
        };
        var grave = new GraveRecordDto
        {
            HeroId = FeatureId,
            Position = position
        };
        var invalid = new[]
        {
            baseSave with { Player = baseSave.Player with { Position = position } },
            baseSave with
            {
                Legacy = baseSave.Legacy with
                {
                    PersistentMap = baseSave.Legacy.PersistentMap with { ObservedPositions = [position] }
                }
            },
            baseSave with
            {
                Legacy = baseSave.Legacy with
                {
                    PersistentMap = baseSave.Legacy.PersistentMap with { VisitedPositions = [position] }
                }
            },
            baseSave with
            {
                Knowledge = baseSave.Knowledge with
                {
                    TeleporterMappings = [mapping with { Source = position }]
                }
            },
            baseSave with
            {
                Knowledge = baseSave.Knowledge with
                {
                    TeleporterMappings = [mapping with { Destination = position }]
                }
            },
            baseSave with { Dungeon = baseSave.Dungeon with { Features = [feature with { Position = position }] } },
            baseSave with { Legacy = baseSave.Legacy with { PreviousHeroes = [hero] } },
            baseSave with { Legacy = baseSave.Legacy with { Graves = [grave] } }
        };

        Assert.All(invalid, save => Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(save)));

        var upperInvalid = new[]
        {
            baseSave with { Player = baseSave.Player with { Position = upperPosition } },
            baseSave with
            {
                Legacy = baseSave.Legacy with
                {
                    PersistentMap = baseSave.Legacy.PersistentMap with { ObservedPositions = [upperPosition] }
                }
            },
            baseSave with
            {
                Legacy = baseSave.Legacy with
                {
                    PersistentMap = baseSave.Legacy.PersistentMap with { VisitedPositions = [upperPosition] }
                }
            },
            baseSave with
            {
                Knowledge = baseSave.Knowledge with
                {
                    TeleporterMappings = [mapping with { Source = upperPosition }]
                }
            },
            baseSave with
            {
                Knowledge = baseSave.Knowledge with
                {
                    TeleporterMappings = [mapping with { Destination = upperPosition }]
                }
            },
            baseSave with { Dungeon = baseSave.Dungeon with { Features = [feature with { Position = upperPosition }] } },
            baseSave with { Legacy = baseSave.Legacy with { PreviousHeroes = [hero with { DeathPosition = upperPosition }] } },
            baseSave with { Legacy = baseSave.Legacy with { Graves = [grave with { Position = upperPosition }] } }
        };

        Assert.All(upperInvalid, save => Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(save)));
    }

    [Fact]
    public void Save_validation_rejects_null_legacy_records_and_nested_values()
    {
        var baseSave = GameStateSaveDto.FromState(GameState.Create(1234));
        var hero = new DeadHeroRecordDto
        {
            HeroId = FeatureId,
            Attributes = new PlayerAttributesDto(),
            DeathPosition = new DungeonPositionDto { Floor = 1 }
        };
        var grave = new GraveRecordDto
        {
            HeroId = FeatureId,
            Position = new DungeonPositionDto { Floor = 1 }
        };
        var invalid = new[]
        {
            baseSave with { Legacy = baseSave.Legacy with { PreviousHeroes = [null!] } },
            baseSave with { Legacy = baseSave.Legacy with { PreviousHeroes = [hero with { Attributes = null! }] } },
            baseSave with { Legacy = baseSave.Legacy with { PreviousHeroes = [hero with { DeathPosition = null! }] } },
            baseSave with { Legacy = baseSave.Legacy with { Graves = [null!] } },
            baseSave with { Legacy = baseSave.Legacy with { Graves = [grave with { Position = null! }] } }
        };

        Assert.All(invalid, save => Assert.Throws<SaveFormatException>(() => SaveMigrations.Validate(save)));
    }

    private static FeatureInstance Feature() => new(
        FeatureId, "feature", new DungeonPosition(1, 0, 0));

    private static GameState ActiveState(FeatureInstance feature) => GameState.Create(1234) with
    {
        Player = new PlayerState { Position = feature.Position },
        Expedition = new ExpeditionState
        {
            Active = true,
            ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000066")
        },
        Dungeon = new DungeonState { Features = [feature] }
    };

    private static TeleporterMapping Mapping(TeleporterMappingStatus status) => new(
        "network",
        "node",
        new DungeonPosition(1, 0, 0),
        new DungeonPosition(2, 1, 1),
        status);

    private static FeatureInstanceDto FeatureDto() => new()
    {
        InstanceId = FeatureId,
        DefinitionId = "feature",
        Position = new DungeonPositionDto { Floor = 1, X = 0, Y = 0 }
    };

    private static KnowledgeEntryDto EntryDto() => new()
    {
        SubjectId = "subject",
        Observations = ["observation"],
        Hypotheses = ["hypothesis"],
        ConfirmedFacts = ["fact"]
    };

    private static TeleporterMappingDto MappingDto() => new()
    {
        NetworkId = "network",
        NodeId = "node",
        Source = new DungeonPositionDto { Floor = 1, X = 0, Y = 0 },
        Destination = new DungeonPositionDto { Floor = 2, X = 1, Y = 1 },
        Status = TeleporterMappingStatus.Observed
    };

    private sealed class DuplicateRuleDictionary : IReadOnlyDictionary<string, string>
    {
        private readonly KeyValuePair<string, string>[] _pairs =
        [
            new("hint", "one"),
            new("hint", "two")
        ];

        public IEnumerable<string> Keys => _pairs.Select(pair => pair.Key);
        public IEnumerable<string> Values => _pairs.Select(pair => pair.Value);
        public int Count => _pairs.Length;
        public string this[string key] => _pairs.First(pair => pair.Key == key).Value;
        public bool ContainsKey(string key) => _pairs.Any(pair => pair.Key == key);
        public bool TryGetValue(string key, out string value)
        {
            var pair = _pairs.FirstOrDefault(item => item.Key == key);
            value = pair.Value;
            return pair.Key is not null;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, string>>)_pairs).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _pairs.GetEnumerator();
    }
}
