using Telengard.Core.Simulation;

namespace Telengard.Core.Economy;

public sealed record AcquireTreasureCommand : ICommand
{
    public AcquireTreasureCommand(int gold, IEnumerable<string>? itemIds = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(gold);

        var items = (itemIds ?? []).ToArray();
        if (items.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Treasure item ids cannot be empty.", nameof(itemIds));
        }

        if (gold == 0 && items.Length == 0)
        {
            throw new ArgumentException("Treasure must contain gold or at least one item.", nameof(itemIds));
        }

        Gold = gold;
        ItemIds = Array.AsReadOnly(items);
    }

    public int Gold { get; }
    public IReadOnlyList<string> ItemIds { get; }
}

public sealed record TreasureAcquiredEvent(int Gold, int ItemCount, int CarriedGold) : IDomainEvent;

public sealed record TreasureItemsSecuredEvent(int ItemCount) : IDomainEvent;

public static class TreasureAcquisitionResolver
{
    public static CommandResult Resolve(GameState state, AcquireTreasureCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (!state.Expedition.Active || state.Inn.IsAtInn)
        {
            throw new InvalidOperationException("Treasure can only be acquired during an active dungeon expedition.");
        }

        if (!state.Player.Alive)
        {
            throw new InvalidOperationException("A dead player cannot acquire treasure.");
        }

        if (state.Player.CarriedGold != state.Expedition.CarriedGold)
        {
            throw new InvalidOperationException("Player and expedition carried gold must match.");
        }

        if (state.Expedition.CarriedGold < 0)
        {
            throw new InvalidOperationException("Carried gold cannot be negative.");
        }

        if (command.Gold > int.MaxValue - state.Expedition.CarriedGold)
        {
            throw new OverflowException("Carried gold exceeds the supported range.");
        }

        var carriedGold = state.Expedition.CarriedGold + command.Gold;
        var acquiredItems = state.Expedition.AcquiredItems.Concat(command.ItemIds).ToArray();
        var nextState = state with
        {
            Player = state.Player with { CarriedGold = carriedGold },
            Expedition = state.Expedition with
            {
                CarriedGold = carriedGold,
                AcquiredItems = acquiredItems
            }
        };

        return new CommandResult(
            nextState,
            [new TreasureAcquiredEvent(command.Gold, command.ItemIds.Count, carriedGold)]);
    }
}
