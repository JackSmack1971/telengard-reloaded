namespace Telengard.Core.Items;

public sealed record ItemObservedState
{
    public ItemObservedState(
        Guid instanceId,
        bool identified,
        IEnumerable<string>? generatedAffixes = null,
        string? curse = null,
        int? durability = null)
    {
        if (instanceId == Guid.Empty)
        {
            throw new ArgumentException("Item instance id cannot be empty.", nameof(instanceId));
        }

        if (durability is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durability));
        }

        InstanceId = instanceId;
        Identified = identified;
        var affixes = (generatedAffixes ?? []).ToArray();
        if (affixes.Any(affix => affix is null))
        {
            throw new ArgumentException("Generated affixes cannot contain null values.", nameof(generatedAffixes));
        }

        GeneratedAffixes = Array.AsReadOnly(affixes);
        Curse = string.IsNullOrWhiteSpace(curse) ? null : curse;
        Durability = durability;
    }

    public Guid InstanceId { get; }
    public bool Identified { get; }
    public IReadOnlyList<string> GeneratedAffixes { get; }
    public string? Curse { get; }
    public int? Durability { get; }
}

public sealed record ItemInstance
{
    public ItemInstance(
        Guid instanceId,
        string definitionId,
        IEnumerable<string>? generatedAffixes = null,
        string? curse = null,
        bool identifiedState = false,
        int durability = 0)
    {
        if (instanceId == Guid.Empty)
        {
            throw new ArgumentException("Item instance id cannot be empty.", nameof(instanceId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(definitionId);
        ArgumentOutOfRangeException.ThrowIfNegative(durability);

        InstanceId = instanceId;
        DefinitionId = definitionId;
        GeneratedAffixes = CopyValues(generatedAffixes);
        Curse = string.IsNullOrWhiteSpace(curse) ? null : curse;
        IdentifiedState = identifiedState;
        Durability = durability;
    }

    public Guid InstanceId { get; }
    public string DefinitionId { get; }
    public IReadOnlyList<string> GeneratedAffixes { get; }
    public string? Curse { get; }
    public bool IdentifiedState { get; }
    public int Durability { get; }

    public ItemObservedState ToObservedState() => IdentifiedState
        ? new(InstanceId, identified: true, GeneratedAffixes, Curse, Durability)
        : new(InstanceId, identified: false);

    public ItemInstance Identify() => IdentifiedState
        ? this
        : new ItemInstance(
            InstanceId,
            DefinitionId,
            GeneratedAffixes,
            Curse,
            identifiedState: true,
            durability: Durability);

    public ItemInstance WithGeneratedAffixes(IEnumerable<string>? affixes) =>
        new(
            InstanceId,
            DefinitionId,
            affixes,
            Curse,
            IdentifiedState,
            Durability);

    public ItemInstance WithCurse(string curse)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(curse);

        return new ItemInstance(
            InstanceId,
            DefinitionId,
            GeneratedAffixes,
            curse,
            IdentifiedState,
            Durability);
    }

    private static IReadOnlyList<string> CopyValues(IEnumerable<string>? values)
    {
        if (values is null) return Array.Empty<string>();

        var copy = new List<string>();
        foreach (var value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (copy.Contains(value, StringComparer.Ordinal))
            {
                throw new ArgumentException("Affixes must be unique.", nameof(values));
            }

            copy.Add(value);
        }

        return Array.AsReadOnly(copy.ToArray());
    }
}
