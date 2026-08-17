using Telengard.Core.Simulation;

namespace Telengard.Core.Combat;

public sealed record MonsterInstance
{
    public MonsterInstance(
        Guid instanceId,
        string definitionId,
        int level,
        int currentHitPoints,
        DungeonPosition position,
        IEnumerable<string>? temporaryEffects = null,
        string? currentBehaviorState = null)
    {
        if (instanceId == Guid.Empty) throw new ArgumentException("Instance ID must not be empty.", nameof(instanceId));
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionId);
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(currentHitPoints);
        ArgumentNullException.ThrowIfNull(position);

        InstanceId = instanceId;
        DefinitionId = definitionId;
        Level = level;
        CurrentHitPoints = currentHitPoints;
        TemporaryEffects = CopyEffects(temporaryEffects);
        CurrentBehaviorState = string.IsNullOrWhiteSpace(currentBehaviorState) ? null : currentBehaviorState;
        Position = position;
    }

    public Guid InstanceId { get; }
    public string DefinitionId { get; }
    public int Level { get; }
    public int CurrentHitPoints { get; init; }
    public IReadOnlyList<string> TemporaryEffects { get; }
    public string? CurrentBehaviorState { get; }
    public DungeonPosition Position { get; }

    private static IReadOnlyList<string> CopyEffects(IEnumerable<string>? effects)
    {
        if (effects is null) return Array.Empty<string>();

        var copy = new List<string>();
        foreach (var effect in effects)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(effect);
            copy.Add(effect);
        }

        return copy.ToArray();
    }
}
