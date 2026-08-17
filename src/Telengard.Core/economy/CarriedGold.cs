using Telengard.Core.Simulation;

namespace Telengard.Core.Economy;

public sealed record AcquireGoldCommand(int Amount) : ICommand;

public sealed record GoldAcquiredEvent(int Amount, int CarriedGold) : IDomainEvent;

public static class CarriedGoldResolver
{
    public static CommandResult Acquire(GameState state, AcquireGoldCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (command.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command.Amount), command.Amount, "Gold acquired must be positive.");
        }

        if (!state.Expedition.Active || state.Inn.IsAtInn)
        {
            throw new InvalidOperationException("Gold can only be acquired during an active dungeon expedition.");
        }

        if (state.Player.CarriedGold != state.Expedition.CarriedGold)
        {
            throw new InvalidOperationException("Player and expedition carried gold must match.");
        }

        if (state.Expedition.CarriedGold < 0)
        {
            throw new InvalidOperationException("Carried gold cannot be negative.");
        }

        if (command.Amount > int.MaxValue - state.Expedition.CarriedGold)
        {
            throw new OverflowException("Carried gold exceeds the supported range.");
        }

        var carriedGold = state.Expedition.CarriedGold + command.Amount;
        var nextState = state with
        {
            Player = state.Player with { CarriedGold = carriedGold },
            Expedition = state.Expedition with { CarriedGold = carriedGold }
        };

        return new CommandResult(nextState, [new GoldAcquiredEvent(command.Amount, carriedGold)]);
    }
}
