using Telengard.Core.Rng;
using Telengard.Core.Simulation;

namespace Telengard.Content;

public sealed class FeatureOutcomeSelectionContext
{
    private readonly HashSet<string> _conditions;

    public FeatureOutcomeSelectionContext(IEnumerable<string>? conditions = null)
    {
        _conditions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var condition in conditions ?? [])
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(condition);
            if (!_conditions.Add(condition))
            {
                throw new ArgumentException("Conditions must be unique.", nameof(conditions));
            }
        }
    }

    public IReadOnlySet<string> Conditions => _conditions;

    internal bool Satisfies(IReadOnlyList<string> requiredConditions) =>
        requiredConditions.All(_conditions.Contains);
}

public static class FeatureOutcomeEngine
{
    public static FeatureOutcome Select(
        FeatureDefinition definition,
        FeatureOutcomeSelectionContext context,
        DeterministicRngStream rng)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(rng);

        var eligible = definition.OutcomeTable
            .Where(outcome => context.Satisfies(outcome.Conditions))
            .Where(outcome => outcome.Weight > 0)
            .ToArray();

        if (eligible.Length == 0)
        {
            throw new InvalidOperationException("The feature has no eligible weighted outcomes.");
        }

        var totalWeight = 0L;
        foreach (var outcome in eligible)
        {
            totalWeight = checked(totalWeight + outcome.Weight);
        }

        var roll = rng.NextLong(0, totalWeight);
        var cumulativeWeight = 0L;
        foreach (var outcome in eligible)
        {
            cumulativeWeight += outcome.Weight;
            if (roll < cumulativeWeight)
            {
                return outcome;
            }
        }

        throw new InvalidOperationException("The feature outcome roll did not resolve.");
    }

    public static FeatureOutcome Select(
        FeatureDefinition definition,
        FeatureOutcomeSelectionContext context,
        long worldSeed,
        string contentVersion,
        Guid featureId,
        DungeonPosition position,
        int activationCount)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(position);
        ArgumentOutOfRangeException.ThrowIfNegative(activationCount);

        var rng = new DeterministicRng(worldSeed, contentVersion).CreateStream(
            "feature-outcome",
            $"definition:{definition.Id}",
            $"feature:{featureId}",
            $"floor:{position.Floor}",
            $"x:{position.X}",
            $"y:{position.Y}",
            $"activation:{activationCount}");
        return Select(definition, context, rng);
    }
}
