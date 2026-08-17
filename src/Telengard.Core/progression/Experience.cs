using Telengard.Core.Simulation;

namespace Telengard.Core.Progression;

public sealed record AwardExperienceCommand : ICommand
{
    public AwardExperienceCommand(long amount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(amount, 1L);
        Amount = amount;
    }

    public long Amount { get; }
}

public sealed record ExperienceAwardedEvent(long Amount, long TotalExperience) : IDomainEvent;

public static class ExperienceResolver
{
    public static CommandResult Award(GameState state, AwardExperienceCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (!state.Inn.IsAtInn || state.Expedition.Active)
        {
            throw new InvalidOperationException("Experience can only be committed at the inn.");
        }

        if (!state.Player.Alive)
        {
            throw new InvalidOperationException("A dead player cannot gain experience.");
        }

        if (state.Player.Experience < 0)
        {
            throw new InvalidOperationException("Experience cannot be negative.");
        }

        if (command.Amount > long.MaxValue - state.Player.Experience)
        {
            throw new OverflowException("Experience exceeds the supported range.");
        }

        var totalExperience = state.Player.Experience + command.Amount;
        return new CommandResult(
            state with { Player = state.Player with { Experience = totalExperience } },
            [new ExperienceAwardedEvent(command.Amount, totalExperience)]);
    }
}
