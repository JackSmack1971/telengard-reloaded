namespace Telengard.Core.Knowledge;

public sealed record KnowledgeEntry
{
    public KnowledgeEntry(
        string subjectId,
        IEnumerable<string>? observations = null,
        int sampleCount = 0,
        IEnumerable<string>? hypotheses = null,
        int confidence = 0,
        IEnumerable<string>? confirmedFacts = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentOutOfRangeException.ThrowIfNegative(sampleCount);
        if (confidence is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), confidence, "Confidence must be between 0 and 100.");
        }

        SubjectId = subjectId;
        Observations = CopyValues(observations, nameof(observations));
        SampleCount = sampleCount;
        Hypotheses = CopyValues(hypotheses, nameof(hypotheses));
        Confidence = confidence;
        ConfirmedFacts = CopyValues(confirmedFacts, nameof(confirmedFacts));
    }

    public string SubjectId { get; }
    public IReadOnlyList<string> Observations { get; }
    public int SampleCount { get; }
    public IReadOnlyList<string> Hypotheses { get; }
    public int Confidence { get; }
    public IReadOnlyList<string> ConfirmedFacts { get; }

    private static IReadOnlyList<string> CopyValues(IEnumerable<string>? values, string parameterName)
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

        return Array.AsReadOnly(copy.ToArray());
    }
}
