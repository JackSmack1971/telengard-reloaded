using Telengard.Core.Simulation;
using Telengard.Core.World.Features;

namespace Telengard.Content;

public static class FountainResolver
{
    public static CommandResult Resolve(
        GameState state,
        ActivateFeatureCommand command,
        FeatureDefinition definition,
        FeatureOutcomeSelectionContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Type != FeatureType.Fountain)
        {
            throw new ArgumentException("The feature definition must describe a fountain.", nameof(definition));
        }

        var feature = state.Dungeon.Features.SingleOrDefault(candidate => candidate.InstanceId == command.FeatureId)
            ?? throw new InvalidOperationException("The requested fountain does not exist in the current dungeon state.");
        if (!string.Equals(feature.DefinitionId, definition.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The fountain definition does not match the dungeon feature.");
        }

        var outcome = FeatureOutcomeEngine.Select(
            definition,
            context ?? new FeatureOutcomeSelectionContext(),
            state.WorldSeed,
            state.Versions.ContentVersion,
            feature.InstanceId,
            feature.Position,
            feature.ActivationCount);

        return FeatureActivationResolver.ActivateFountain(
            state,
            command,
            new FeatureOutcomeResolution(outcome.Effects, outcome.Observations));
    }
}
