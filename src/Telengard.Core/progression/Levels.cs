using Telengard.Core.Simulation;

namespace Telengard.Core.Progression;

public sealed record LevelConfiguration
{
    private readonly IReadOnlyList<long> _experienceThresholds;

    public LevelConfiguration(IEnumerable<long> experienceThresholds)
    {
        ArgumentNullException.ThrowIfNull(experienceThresholds);

        var thresholds = experienceThresholds.ToArray();
        if (thresholds.Length == 0)
        {
            throw new ArgumentException("At least one level threshold is required.", nameof(experienceThresholds));
        }

        if (thresholds[0] != 0)
        {
            throw new ArgumentException("The first level threshold must be zero.", nameof(experienceThresholds));
        }

        if (thresholds.Any(threshold => threshold < 0) ||
            thresholds.Zip(thresholds.Skip(1)).Any(pair => pair.First >= pair.Second))
        {
            throw new ArgumentException("Level thresholds must be nonnegative and strictly increasing.", nameof(experienceThresholds));
        }

        _experienceThresholds = Array.AsReadOnly(thresholds);
    }

    public IReadOnlyList<long> ExperienceThresholds => _experienceThresholds;

    public int MaximumLevel => _experienceThresholds.Count;

    public long GetRequiredExperience(int level)
    {
        if (level is < 1 || level > MaximumLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Level is outside the configured range.");
        }

        return _experienceThresholds[level - 1];
    }
}

public sealed record LevelUpCommand : ICommand;

public sealed record PlayerLeveledUpEvent(
    int PreviousLevel,
    int Level,
    long Experience) : IDomainEvent;

public static class LevelResolver
{
    public static CommandResult LevelUp(
        GameState state,
        LevelUpCommand command,
        LevelConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(configuration);

        if (!state.Inn.IsAtInn || state.Expedition.Active)
        {
            throw new InvalidOperationException("A player can only level up at the inn.");
        }

        if (!state.Player.Alive)
        {
            throw new InvalidOperationException("A dead player cannot level up.");
        }

        if (state.Player.Experience < 0)
        {
            throw new InvalidOperationException("Experience cannot be negative.");
        }

        if (state.Player.Level < 1)
        {
            throw new InvalidOperationException("Player level must be at least one.");
        }

        if (state.Player.Level == int.MaxValue)
        {
            throw new InvalidOperationException("The player is already at the maximum level.");
        }

        var nextLevel = state.Player.Level + 1;
        if (nextLevel > configuration.MaximumLevel)
        {
            throw new InvalidOperationException("The player is already at the maximum configured level.");
        }

        var requiredExperience = configuration.GetRequiredExperience(nextLevel);
        if (state.Player.Experience < requiredExperience)
        {
            throw new InvalidOperationException("The player does not have enough experience to level up.");
        }

        return new CommandResult(
            state with { Player = state.Player with { Level = nextLevel } },
            [new PlayerLeveledUpEvent(state.Player.Level, nextLevel, state.Player.Experience)]);
    }
}
