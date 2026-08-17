using Telengard.Core.Items;

namespace Telengard.Save.Dto;

public sealed record ItemInstanceDto
{
    public Guid InstanceId { get; init; }
    public required string DefinitionId { get; init; }
    public IReadOnlyList<string>? GeneratedAffixes { get; init; }
    public string? Curse { get; init; }
    public bool IdentifiedState { get; init; }
    public int Durability { get; init; }

    public static ItemInstanceDto FromState(ItemInstance item) => new()
    {
        InstanceId = item.InstanceId,
        DefinitionId = item.DefinitionId,
        GeneratedAffixes = item.GeneratedAffixes.ToArray(),
        Curse = item.Curse,
        IdentifiedState = item.IdentifiedState,
        Durability = item.Durability
    };

    public ItemInstance ToState() => new(
        InstanceId,
        DefinitionId,
        GeneratedAffixes ?? [],
        Curse,
        IdentifiedState,
        Durability);
}
