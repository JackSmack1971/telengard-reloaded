using Telengard.Core.Magic;

namespace Telengard.Content;

public sealed record SpellDefinition : ISpellDefinition
{
    public SpellDefinition(
        string id,
        string name,
        string initialDescription,
        IEnumerable<string>? discoveredDescriptions,
        int cost,
        string targetingRule,
        IEnumerable<string>? effects = null,
        IEnumerable<string>? interactions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(initialDescription);
        ArgumentOutOfRangeException.ThrowIfNegative(cost);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetingRule);

        Id = id;
        Name = name;
        InitialDescription = initialDescription;
        DiscoveredDescriptions = CopyTags(discoveredDescriptions, nameof(discoveredDescriptions));
        Cost = cost;
        TargetingRule = targetingRule;
        Effects = CopyTags(effects, nameof(effects));
        Interactions = CopyTags(interactions, nameof(interactions));
    }

    public string Id { get; }
    public string Name { get; }
    public string InitialDescription { get; }
    public IReadOnlyList<string> DiscoveredDescriptions { get; }
    public int Cost { get; }
    public string TargetingRule { get; }
    public IReadOnlyList<string> Effects { get; }
    public IReadOnlyList<string> Interactions { get; }

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
