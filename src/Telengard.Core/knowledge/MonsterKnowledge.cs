using Telengard.Core.Simulation;

namespace Telengard.Core.Knowledge;

public static class MonsterKnowledgeSubject
{
    public const string Prefix = "monster:";

    public static string ForDefinition(string monsterDefinitionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(monsterDefinitionId);
        return Prefix + monsterDefinitionId;
    }
}

public sealed record AddMonsterKnowledgeCommand : ICommand
{
    public AddMonsterKnowledgeCommand(
        string monsterDefinitionId,
        IEnumerable<string> observations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(monsterDefinitionId);
        ArgumentNullException.ThrowIfNull(observations);

        MonsterDefinitionId = monsterDefinitionId;
        var observationCommand = new AddKnowledgeObservationCommand(
            MonsterKnowledgeSubject.ForDefinition(monsterDefinitionId),
            observations);
        Observations = observationCommand.Observations;
    }

    public string MonsterDefinitionId { get; }
    public IReadOnlyList<string> Observations { get; }
}

public static class MonsterKnowledgeResolver
{
    public static CommandResult Add(
        GameState state,
        AddMonsterKnowledgeCommand command,
        KnowledgeConfidenceConfiguration? confidenceConfiguration = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        return KnowledgeObservationResolver.Add(
            state,
            new AddKnowledgeObservationCommand(
                MonsterKnowledgeSubject.ForDefinition(command.MonsterDefinitionId),
                command.Observations),
            confidenceConfiguration);
    }

    public static bool IsKnown(
        KnowledgeState knowledge,
        string monsterDefinitionId)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        var subjectId = MonsterKnowledgeSubject.ForDefinition(monsterDefinitionId);
        return knowledge.Entries.Any(entry =>
            string.Equals(entry.SubjectId, subjectId, StringComparison.Ordinal) &&
            entry.SampleCount > 0);
    }
}
