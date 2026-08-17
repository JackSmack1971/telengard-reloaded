using Telengard.Core.Simulation;

namespace Telengard.Core.World.Features;

public sealed record FeatureInstance
{
    public FeatureInstance(
        Guid instanceId,
        string definitionId,
        DungeonPosition position,
        int activationCount = 0,
        bool discovered = false)
    {
        if (instanceId == Guid.Empty) throw new ArgumentException("Feature instance id cannot be empty.", nameof(instanceId));
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionId);
        ArgumentNullException.ThrowIfNull(position);
        ArgumentOutOfRangeException.ThrowIfNegative(activationCount);

        InstanceId = instanceId;
        DefinitionId = definitionId;
        Position = position;
        ActivationCount = activationCount;
        Discovered = discovered;
    }

    public Guid InstanceId { get; }
    public string DefinitionId { get; }
    public DungeonPosition Position { get; }
    public int ActivationCount { get; }
    public bool Discovered { get; }

    public FeatureInstance Activate() =>
        ActivationCount == int.MaxValue
            ? throw new OverflowException("Feature activation count exceeds the supported range.")
            : new FeatureInstance(InstanceId, DefinitionId, Position, ActivationCount + 1, true);
}

public sealed record ActivateFeatureCommand : ICommand
{
    public ActivateFeatureCommand(Guid featureId)
    {
        if (featureId == Guid.Empty) throw new ArgumentException("Feature id cannot be empty.", nameof(featureId));
        FeatureId = featureId;
    }

    public Guid FeatureId { get; }
}

public sealed record FeatureDiscoveredEvent(Guid FeatureId, DungeonPosition Position) : IDomainEvent;

public sealed record FeatureActivatedEvent(
    Guid FeatureId,
    DungeonPosition Position,
    int ActivationCount) : IDomainEvent;

public sealed record FeatureOutcomeResolvedEvent(
    Guid FeatureId,
    DungeonPosition Position,
    int ActivationCount,
    IReadOnlyList<string> Effects,
    IReadOnlyList<string> Observations) : IDomainEvent;

public sealed record FeatureOutcomeResolution
{
    public FeatureOutcomeResolution(
        IEnumerable<string>? effects = null,
        IEnumerable<string>? observations = null)
    {
        Effects = CopyTags(effects, nameof(effects));
        Observations = CopyTags(observations, nameof(observations));
    }

    public IReadOnlyList<string> Effects { get; }
    public IReadOnlyList<string> Observations { get; }

    private static IReadOnlyList<string> CopyTags(IEnumerable<string>? values, string parameterName)
    {
        if (values is null) return Array.Empty<string>();

        var copy = new List<string>();
        foreach (var value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (!copy.Contains(value, StringComparer.Ordinal))
            {
                copy.Add(value);
            }
            else
            {
                throw new ArgumentException("Values must be unique.", parameterName);
            }
        }

        return copy.ToArray();
    }
}

public sealed record FountainOutcomeResolvedEvent(
    Guid FeatureId,
    DungeonPosition Position,
    int ActivationCount,
    IReadOnlyList<string> Effects,
    IReadOnlyList<string> Observations) : IDomainEvent;

public sealed record AltarOutcomeResolvedEvent(
    Guid FeatureId,
    DungeonPosition Position,
    int ActivationCount,
    IReadOnlyList<string> Effects,
    IReadOnlyList<string> Observations) : IDomainEvent;

public sealed record PitOutcomeResolvedEvent(
    Guid FeatureId,
    DungeonPosition Position,
    int ActivationCount,
    IReadOnlyList<string> Effects,
    IReadOnlyList<string> Observations) : IDomainEvent;

public sealed record TeleporterOutcomeResolvedEvent(
    Guid FeatureId,
    DungeonPosition From,
    DungeonPosition To,
    int ActivationCount,
    IReadOnlyList<string> Effects,
    IReadOnlyList<string> Observations) : IDomainEvent;

public static class FountainEffectIds
{
    public const string RestoreSpellPower = "restore_spell_power";
    public const string Blindness = "blindness";
    public const string CleansePoison = "cleanse_poison";
    public const string UnknownTransformation = "unknown_transformation";
}

public static class PitEffectIds
{
    public const string DropTwoFloors = "drop_two_floors";
}

public static class FeatureActivationResolver
{
    public static CommandResult Activate(GameState state, ActivateFeatureCommand command) =>
        Activate(state, command, null);

    public static CommandResult Activate(
        GameState state,
        ActivateFeatureCommand command,
        FeatureOutcomeResolution? outcome)
        => Activate(state, command, outcome, FeatureOutcomeKind.Generic);

    public static CommandResult ActivateFountain(
        GameState state,
        ActivateFeatureCommand command,
        FeatureOutcomeResolution outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return Activate(state, command, outcome, FeatureOutcomeKind.Fountain);
    }

    public static CommandResult ActivateAltar(
        GameState state,
        ActivateFeatureCommand command,
        FeatureOutcomeResolution outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return Activate(state, command, outcome, FeatureOutcomeKind.Altar);
    }

    public static CommandResult ActivatePit(
        GameState state,
        ActivateFeatureCommand command,
        FeatureOutcomeResolution outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return Activate(state, command, outcome, FeatureOutcomeKind.Pit);
    }

    public static CommandResult ActivateTeleporter(
        GameState state,
        ActivateFeatureCommand command,
        DungeonPosition destination,
        FeatureOutcomeResolution outcome)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(outcome);
        return Activate(state, command, outcome, FeatureOutcomeKind.Teleporter, destination);
    }

    private static CommandResult Activate(
        GameState state,
        ActivateFeatureCommand command,
        FeatureOutcomeResolution? outcome,
        FeatureOutcomeKind outcomeKind,
        DungeonPosition? destination = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (!state.Expedition.Active) throw new InvalidOperationException("Feature activation requires an active expedition.");
        if (!state.Player.Alive) throw new InvalidOperationException("A dead player cannot activate a feature.");
        if (state.Combat is not null) throw new InvalidOperationException("A feature cannot be activated during combat.");

        var feature = state.Dungeon.Features.SingleOrDefault(candidate => candidate.InstanceId == command.FeatureId)
            ?? throw new InvalidOperationException("The requested feature does not exist in the current dungeon state.");
        if (feature.Position != state.Player.Position)
        {
            throw new InvalidOperationException("The player must be at the feature position to activate it.");
        }

        if (outcomeKind == FeatureOutcomeKind.Teleporter)
        {
            ArgumentNullException.ThrowIfNull(destination);
        }

        var activated = feature.Activate();
        var nextFeatures = state.Dungeon.Features
            .Select(candidate => candidate.InstanceId == feature.InstanceId ? activated : candidate)
            .ToArray();
        var next = outcome is null
            ? state with { Dungeon = state.Dungeon with { Features = nextFeatures } }
            : ApplyOutcome(
                state with { Dungeon = state.Dungeon with { Features = nextFeatures } },
                outcome,
                outcomeKind,
                destination);
        var events = new List<IDomainEvent>(2);
        if (!feature.Discovered) events.Add(new FeatureDiscoveredEvent(feature.InstanceId, feature.Position));
        events.Add(new FeatureActivatedEvent(feature.InstanceId, feature.Position, activated.ActivationCount));
        if (outcome is not null)
        {
            events.Add(outcomeKind switch
            {
                FeatureOutcomeKind.Generic => new FeatureOutcomeResolvedEvent(
                    feature.InstanceId,
                    feature.Position,
                    activated.ActivationCount,
                    outcome.Effects,
                    outcome.Observations),
                FeatureOutcomeKind.Fountain => new FountainOutcomeResolvedEvent(
                    feature.InstanceId,
                    feature.Position,
                    activated.ActivationCount,
                    outcome.Effects,
                    outcome.Observations),
                FeatureOutcomeKind.Altar => new AltarOutcomeResolvedEvent(
                    feature.InstanceId,
                    feature.Position,
                    activated.ActivationCount,
                    outcome.Effects,
                    outcome.Observations),
                FeatureOutcomeKind.Pit => new PitOutcomeResolvedEvent(
                    feature.InstanceId,
                    feature.Position,
                    activated.ActivationCount,
                    outcome.Effects,
                    outcome.Observations),
                FeatureOutcomeKind.Teleporter => new TeleporterOutcomeResolvedEvent(
                    feature.InstanceId,
                    feature.Position,
                    destination!,
                    activated.ActivationCount,
                    outcome.Effects,
                    outcome.Observations),
                _ => throw new InvalidOperationException("Unsupported feature outcome kind.")
            });
        }

        return new CommandResult(next, events);
    }

    private static GameState ApplyOutcome(
        GameState state,
        FeatureOutcomeResolution outcome,
        FeatureOutcomeKind outcomeKind,
        DungeonPosition? destination)
    {
        var player = state.Player;
        var expedition = state.Expedition;
        var temporaryEffects = player.TemporaryEffects.ToList();
        if (outcomeKind is FeatureOutcomeKind.Generic or FeatureOutcomeKind.Altar)
        {
            return state;
        }

        if (outcomeKind == FeatureOutcomeKind.Teleporter)
        {
            var teleporterDestination = destination
                ?? throw new InvalidOperationException("A teleporter destination is required.");
            player = player with { Position = teleporterDestination };
            var floorsVisited = expedition.FloorsVisited.Contains(teleporterDestination.Floor)
                ? expedition.FloorsVisited
                : [.. expedition.FloorsVisited, teleporterDestination.Floor];
            expedition = expedition with
            {
                DeepestFloorReached = Math.Max(expedition.DeepestFloorReached, teleporterDestination.Floor),
                FloorsVisited = floorsVisited
            };
        }

        foreach (var effect in outcome.Effects)
        {
            switch (outcomeKind, effect)
            {
                case (FeatureOutcomeKind.Fountain, FountainEffectIds.RestoreSpellPower):
                    player = player with { SpellPower = player.MaxSpellPower };
                    break;
                case (FeatureOutcomeKind.Fountain, FountainEffectIds.Blindness):
                    if (!temporaryEffects.Contains(FountainEffectIds.Blindness, StringComparer.Ordinal))
                    {
                        temporaryEffects.Add(FountainEffectIds.Blindness);
                    }

                    break;
                case (FeatureOutcomeKind.Fountain, FountainEffectIds.CleansePoison):
                    temporaryEffects.RemoveAll(effect => string.Equals(effect, "poison", StringComparison.Ordinal));
                    break;
                case (FeatureOutcomeKind.Fountain, FountainEffectIds.UnknownTransformation):
                    break;
                case (FeatureOutcomeKind.Pit, PitEffectIds.DropTwoFloors):
                    if (player.Position.Floor > 48)
                    {
                        throw new InvalidOperationException("The pit does not lead to a valid dungeon floor.");
                    }

                    var pitDestination = new DungeonPosition(player.Position.Floor + 2, player.Position.X, player.Position.Y);
                    player = player with { Position = pitDestination };
                    var floorsVisited = expedition.FloorsVisited.Contains(pitDestination.Floor)
                        ? expedition.FloorsVisited
                        : [.. expedition.FloorsVisited, pitDestination.Floor];
                    expedition = expedition with
                    {
                        DeepestFloorReached = Math.Max(expedition.DeepestFloorReached, pitDestination.Floor),
                        FloorsVisited = floorsVisited
                    };
                    break;
                case (FeatureOutcomeKind.Teleporter, _):
                    // Teleporter outcome tags remain content-defined until their
                    // destination and knowledge rules are specified.
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported {outcomeKind.ToString().ToLowerInvariant()} effect: {effect}.");
            }
        }

        return state with
        {
            Player = player with { TemporaryEffects = temporaryEffects.ToArray() },
            Expedition = expedition
        };
    }

    private enum FeatureOutcomeKind
    {
        Generic,
        Fountain,
        Altar,
        Pit,
        Teleporter
    }
}
