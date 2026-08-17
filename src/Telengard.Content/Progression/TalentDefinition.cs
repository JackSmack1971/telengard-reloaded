namespace Telengard.Content;

public sealed record TalentDefinition
{
    public TalentDefinition(
        string id,
        string constellation,
        IEnumerable<string>? prerequisites,
        IEnumerable<string>? effects,
        int cost)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(constellation);
        ArgumentOutOfRangeException.ThrowIfNegative(cost);

        Id = id;
        Constellation = constellation;
        Prerequisites = CopyTags(prerequisites, nameof(prerequisites));
        Effects = CopyTags(effects, nameof(effects));
        Cost = cost;
    }

    public string Id { get; }
    public string Constellation { get; }
    public IReadOnlyList<string> Prerequisites { get; }
    public IReadOnlyList<string> Effects { get; }
    public int Cost { get; }

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
