using System.Globalization;
using System.Text;
using Telengard.Core.Combat;
using Telengard.Core.Economy;
using Telengard.Core.Items;
using Telengard.Core.Knowledge;
using Telengard.Core.Magic;
using Telengard.Core.Presentation;
using Telengard.Core.Progression;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Core.World.Generation;

namespace Telengard.Terminal;

/// <summary>
/// Renders the immutable presentation state as a stable ASCII/symbolic frame.
/// It does not resolve commands or access authoritative simulation state.
/// </summary>
public static class TerminalRenderer
{
    public static string Render(
        PresentationState state,
        IEnumerable<IDomainEvent>? committedEvents = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        var events = committedEvents?.ToArray() ?? [];
        if (events.Any(domainEvent => domainEvent is null))
        {
            throw new ArgumentException("Committed events cannot contain null values.", nameof(committedEvents));
        }

        var output = new StringBuilder();
        AppendLine(output, $"SCENE {(state.IsAtInn ? "INN" : "DUNGEON")}");
        AppendLine(output, $"TICK {state.SimulationTick.ToString(CultureInfo.InvariantCulture)}");
        AppendLine(output, $"POSITION {FormatPosition(state.Player.Position)}");
        AppendLine(output, $"PLAYER id={state.Player.Id:N} level={state.Player.Level.ToString(CultureInfo.InvariantCulture)} " +
            $"hp={state.Player.HitPoints.ToString(CultureInfo.InvariantCulture)}/{state.Player.MaxHitPoints.ToString(CultureInfo.InvariantCulture)} " +
            $"sp={state.Player.SpellPower.ToString(CultureInfo.InvariantCulture)}/{state.Player.MaxSpellPower.ToString(CultureInfo.InvariantCulture)} " +
            $"carried_gold={state.Player.CarriedGold.ToString(CultureInfo.InvariantCulture)} " +
            $"secured_gold={state.SecuredGold.ToString(CultureInfo.InvariantCulture)} alive={state.Player.Alive}");
        AppendLine(output, $"EXPEDITION active={state.Expedition.Active} id={FormatId(state.Expedition.ExpeditionId)} " +
            $"floor={state.Expedition.StartingFloor.ToString(CultureInfo.InvariantCulture)}..{state.Expedition.DeepestFloorReached.ToString(CultureInfo.InvariantCulture)} " +
            $"carried_gold={state.Expedition.CarriedGold.ToString(CultureInfo.InvariantCulture)} " +
            $"items={state.Expedition.AcquiredItems.Count.ToString(CultureInfo.InvariantCulture)}");

        foreach (var mapLine in CreateMapLines(state)) AppendLine(output, mapLine);
        foreach (var feature in state.DiscoveredFeatures
                     .OrderBy(feature => feature.Position.Floor)
                     .ThenBy(feature => feature.Position.X)
                     .ThenBy(feature => feature.Position.Y)
                     .ThenBy(feature => feature.InstanceId))
        {
            AppendLine(output, $"FEATURE id={feature.InstanceId:N} definition={feature.DefinitionId} " +
                $"position={FormatPosition(feature.Position)} activations={feature.ActivationCount.ToString(CultureInfo.InvariantCulture)}");
        }

        foreach (var entry in state.Knowledge.OrderBy(entry => entry.SubjectId, StringComparer.Ordinal))
        {
            AppendLine(output, $"KNOWLEDGE subject={entry.SubjectId} samples={entry.SampleCount.ToString(CultureInfo.InvariantCulture)} " +
                $"confidence={entry.Confidence.ToString(CultureInfo.InvariantCulture)}");
        }

        if (state.Combat is not null)
        {
            AppendLine(output, $"COMBAT id={state.Combat.EncounterId:N} phase={state.Combat.Phase} " +
                $"round={state.Combat.Round.ToString(CultureInfo.InvariantCulture)} threat={state.Combat.ThreatLevel?.ToString() ?? "UNKNOWN"} " +
                $"monster={state.Combat.Monster.DefinitionId} hp={state.Combat.Monster.CurrentHitPoints.ToString(CultureInfo.InvariantCulture)} " +
                $"position={FormatPosition(state.Combat.Monster.Position)}");
        }

        foreach (var domainEvent in events)
        {
            var cue = CreateCue(domainEvent);
            if (cue is not null) AppendLine(output, cue);
        }

        return output.ToString();
    }

    private static IEnumerable<string> CreateMapLines(PresentationState state)
    {
        var observed = state.ObservedMap.ToHashSet();
        var visited = state.VisitedMap.ToHashSet();

        return observed
            .Concat(visited)
            .Append(state.Player.Position)
            .Distinct()
            .OrderBy(position => position.Floor)
            .ThenBy(position => position.X)
            .ThenBy(position => position.Y)
            .Select(position =>
            {
                var marker = position == state.Player.Position ? '@' :
                    visited.Contains(position) ? 'v' : '.';
                return $"MAP {marker} {FormatPosition(position)}";
            });
    }

    private static string? CreateCue(IDomainEvent domainEvent) => domainEvent switch
    {
        DungeonEnteredEvent entered => $"EVENT dungeon_entered {FormatPosition(entered.Position)}",
        PlayerMovedEvent moved => $"EVENT player_moved {FormatPosition(moved.To)}",
        FloorChangedEvent floorChanged => $"EVENT floor_changed {FormatPosition(floorChanged.To)}",
        DungeonLeftEvent left => $"EVENT dungeon_left {FormatPosition(left.Position)}",
        FeatureDiscoveredEvent discovered => $"EVENT feature_discovered {FormatPosition(discovered.Position)}",
        FeatureActivatedEvent activated => $"EVENT feature_activated {FormatPosition(activated.Position)}",
        EncounterStartedEvent => "EVENT encounter_started",
        EncounterEndedEvent => "EVENT encounter_ended",
        CombatPhaseChangedEvent changed => $"EVENT combat_phase_changed {changed.To}",
        MonsterDamagedEvent damaged => $"EVENT monster_damaged amount={damaged.Amount.ToString(CultureInfo.InvariantCulture)}",
        MonsterKilledEvent => "EVENT monster_killed",
        SpellCastEvent => "EVENT spell_cast",
        ItemIdentifiedEvent => "EVENT item_identified",
        ItemEquippedEvent => "EVENT item_equipped",
        ItemUnequippedEvent => "EVENT item_unequipped",
        GoldAcquiredEvent gold => $"EVENT gold_acquired amount={gold.Amount.ToString(CultureInfo.InvariantCulture)}",
        GoldSecuredEvent gold => $"EVENT gold_secured amount={gold.Amount.ToString(CultureInfo.InvariantCulture)}",
        PlayerLeveledUpEvent level => $"EVENT player_leveled_up level={level.Level.ToString(CultureInfo.InvariantCulture)}",
        ExperienceAwardedEvent experience => $"EVENT experience_awarded amount={experience.Amount.ToString(CultureInfo.InvariantCulture)}",
        PlayerDiedEvent died => $"EVENT player_died {FormatPosition(died.Position)}",
        ExpeditionSucceededEvent => "EVENT expedition_succeeded",
        ExpeditionFailedEvent => "EVENT expedition_failed",
        KnowledgeObservationAddedEvent => "EVENT knowledge_updated",
        KnowledgeSampleCountedEvent => "EVENT knowledge_updated",
        KnowledgeConfidenceUpdatedEvent => "EVENT knowledge_updated",
        _ => null
    };

    private static string FormatPosition(DungeonPosition position) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{position.Floor}/{position.X}/{position.Y}");

    private static string FormatId(Guid? id) => id?.ToString("N", CultureInfo.InvariantCulture) ?? "NONE";

    private static void AppendLine(StringBuilder output, string line) => output.Append(line).Append('\n');
}
