using System.Collections.ObjectModel;

namespace Telengard.Content;

public enum FeatureType
{
    Fountain,
    Altar,
    Teleporter,
    Pit,
    Throne,
    Cube,
    Elevator,
    Box,
    Shrine,
    Machine,
    UnknownPhenomenon
}

public sealed record FeatureOutcome
{
    public FeatureOutcome(
        IEnumerable<string>? conditions = null,
        int weight = 0,
        IEnumerable<string>? effects = null,
        IEnumerable<string>? observations = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(weight);

        Conditions = CopyTags(conditions, nameof(conditions));
        Weight = weight;
        Effects = CopyTags(effects, nameof(effects));
        Observations = CopyTags(observations, nameof(observations));
    }

    public IReadOnlyList<string> Conditions { get; }
    public int Weight { get; }
    public IReadOnlyList<string> Effects { get; }
    public IReadOnlyList<string> Observations { get; }

    private static IReadOnlyList<string> CopyTags(IEnumerable<string>? values, string parameterName)
    {
        if (values is null) return Array.Empty<string>();

        var copy = new List<string>();
        foreach (var value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (copy.Contains(value, StringComparer.Ordinal))
            {
                throw new ArgumentException("Values must be unique.", parameterName);
            }

            copy.Add(value);
        }

        return copy.ToArray();
    }
}

public sealed record FeatureDefinition
{
    public FeatureDefinition(
        string id,
        FeatureType type,
        string presentationKey,
        IEnumerable<string>? interactionOptions = null,
        IEnumerable<FeatureOutcome>? outcomeTable = null,
        IReadOnlyDictionary<string, string>? hintRules = null,
        string? knowledgeCategory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown feature type.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(presentationKey);

        Id = id;
        Type = type;
        PresentationKey = presentationKey;
        InteractionOptions = CopyTags(interactionOptions, nameof(interactionOptions));
        OutcomeTable = CopyOutcomes(outcomeTable);
        HintRules = CopyRules(hintRules, nameof(hintRules));
        KnowledgeCategory = string.IsNullOrWhiteSpace(knowledgeCategory) ? null : knowledgeCategory;
    }

    public string Id { get; }
    public FeatureType Type { get; }
    public string PresentationKey { get; }
    public IReadOnlyList<string> InteractionOptions { get; }
    public IReadOnlyList<FeatureOutcome> OutcomeTable { get; }
    public IReadOnlyDictionary<string, string> HintRules { get; }
    public string? KnowledgeCategory { get; }

    private static IReadOnlyList<string> CopyTags(IEnumerable<string>? values, string parameterName)
    {
        if (values is null) return Array.Empty<string>();

        var copy = new List<string>();
        foreach (var value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (copy.Contains(value, StringComparer.Ordinal))
            {
                throw new ArgumentException("Values must be unique.", parameterName);
            }

            copy.Add(value);
        }

        return copy.ToArray();
    }

    private static IReadOnlyList<FeatureOutcome> CopyOutcomes(IEnumerable<FeatureOutcome>? outcomes)
    {
        if (outcomes is null) return Array.Empty<FeatureOutcome>();

        var copy = new List<FeatureOutcome>();
        foreach (var outcome in outcomes)
        {
            ArgumentNullException.ThrowIfNull(outcome);
            copy.Add(outcome);
        }

        return copy.ToArray();
    }

    private static IReadOnlyDictionary<string, string> CopyRules(
        IReadOnlyDictionary<string, string>? rules,
        string parameterName)
    {
        if (rules is null)
        {
            return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));
        }

        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in rules)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Value);
            if (!copy.TryAdd(pair.Key, pair.Value))
            {
                throw new ArgumentException("Hint-rule names must be unique.", parameterName);
            }
        }

        return new ReadOnlyDictionary<string, string>(copy);
    }
}
