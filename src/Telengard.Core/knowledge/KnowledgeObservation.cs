using Telengard.Core.Simulation;

namespace Telengard.Core.Knowledge;

public sealed record AddKnowledgeObservationCommand : ICommand
{
    public AddKnowledgeObservationCommand(string subjectId, IEnumerable<string> observations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentNullException.ThrowIfNull(observations);

        SubjectId = subjectId;
        Observations = CopyObservations(observations);
        if (Observations.Count == 0)
        {
            throw new ArgumentException("At least one observation is required.", nameof(observations));
        }
    }

    public string SubjectId { get; }
    public IReadOnlyList<string> Observations { get; }

    private static IReadOnlyList<string> CopyObservations(IEnumerable<string> observations)
    {
        var copy = new List<string>();
        foreach (var observation in observations)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(observation);
            if (!copy.Contains(observation, StringComparer.Ordinal))
            {
                copy.Add(observation);
            }
            else
            {
                throw new ArgumentException("Observations must be unique.", nameof(observations));
            }
        }

        return Array.AsReadOnly(copy.ToArray());
    }
}

public sealed record KnowledgeObservationAddedEvent(
    string SubjectId,
    IReadOnlyList<string> Observations) : IDomainEvent;

public sealed record KnowledgeSampleCountedEvent(
    string SubjectId,
    int SampleCount) : IDomainEvent;

public sealed record KnowledgeConfidenceUpdatedEvent(
    string SubjectId,
    int SampleCount,
    int Confidence) : IDomainEvent;

public static class KnowledgeObservationResolver
{
    public static CommandResult Add(
        GameState state,
        AddKnowledgeObservationCommand command,
        KnowledgeConfidenceConfiguration? confidenceConfiguration = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        confidenceConfiguration ??= new KnowledgeConfidenceConfiguration();

        if (!state.Expedition.Active)
        {
            throw new InvalidOperationException("Knowledge observations require an active expedition.");
        }

        if (!state.Player.Alive)
        {
            throw new InvalidOperationException("A dead player cannot add a knowledge observation.");
        }

        var existing = state.Knowledge.Entries.SingleOrDefault(entry =>
            string.Equals(entry.SubjectId, command.SubjectId, StringComparison.Ordinal));
        var newObservations = command.Observations
            .Where(observation => existing is null ||
                !existing.Observations.Contains(observation, StringComparer.Ordinal))
            .ToArray();

        var addedObservations = Array.AsReadOnly(newObservations);

        var sampleCount = existing is null
            ? 1
            : existing.SampleCount == int.MaxValue
                ? throw new OverflowException("Knowledge sample count exceeds the supported range.")
                : existing.SampleCount + 1;
        var confidence = confidenceConfiguration.Resolve(sampleCount);
        var updated = existing is null
            ? new KnowledgeEntry(command.SubjectId, addedObservations, sampleCount, confidence: confidence)
            : new KnowledgeEntry(
                existing.SubjectId,
                existing.Observations.Concat(addedObservations),
                sampleCount,
                existing.Hypotheses,
                confidence,
                existing.ConfirmedFacts);
        var entries = existing is null
            ? state.Knowledge.Entries.Append(updated)
            : state.Knowledge.Entries.Select(entry =>
                string.Equals(entry.SubjectId, command.SubjectId, StringComparison.Ordinal)
                    ? updated
                    : entry);
        var nextState = state with { Knowledge = new KnowledgeState(entries) };

        var events = new List<IDomainEvent>
        {
            new KnowledgeSampleCountedEvent(command.SubjectId, updated.SampleCount)
        };
        if (newObservations.Length > 0)
        {
            events.Insert(0, new KnowledgeObservationAddedEvent(command.SubjectId, addedObservations));
        }
        if (existing is null || existing.Confidence != confidence)
        {
            events.Add(new KnowledgeConfidenceUpdatedEvent(command.SubjectId, sampleCount, confidence));
        }

        return new CommandResult(nextState, events);
    }
}
