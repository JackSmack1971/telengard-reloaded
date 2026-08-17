using Telengard.Core.Simulation;

namespace Telengard.Core.Knowledge;

public static class FeatureKnowledgeSubject
{
    public const string Prefix = "feature:";

    public static string ForDefinition(string featureDefinitionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureDefinitionId);
        return Prefix + featureDefinitionId;
    }
}

public sealed record AddFeatureKnowledgeCommand : ICommand
{
    public AddFeatureKnowledgeCommand(
        string featureDefinitionId,
        IEnumerable<string> observations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureDefinitionId);
        ArgumentNullException.ThrowIfNull(observations);

        FeatureDefinitionId = featureDefinitionId;
        var observationCommand = new AddKnowledgeObservationCommand(
            FeatureKnowledgeSubject.ForDefinition(featureDefinitionId),
            observations);
        Observations = observationCommand.Observations;
    }

    public string FeatureDefinitionId { get; }
    public IReadOnlyList<string> Observations { get; }
}

public static class FeatureKnowledgeResolver
{
    public static CommandResult Add(
        GameState state,
        AddFeatureKnowledgeCommand command,
        KnowledgeConfidenceConfiguration? confidenceConfiguration = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        return KnowledgeObservationResolver.Add(
            state,
            new AddKnowledgeObservationCommand(
                FeatureKnowledgeSubject.ForDefinition(command.FeatureDefinitionId),
                command.Observations),
            confidenceConfiguration);
    }

    public static bool IsKnown(
        KnowledgeState knowledge,
        string featureDefinitionId)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        var subjectId = FeatureKnowledgeSubject.ForDefinition(featureDefinitionId);
        return knowledge.Entries.Any(entry =>
            string.Equals(entry.SubjectId, subjectId, StringComparison.Ordinal) &&
            entry.SampleCount > 0);
    }
}
