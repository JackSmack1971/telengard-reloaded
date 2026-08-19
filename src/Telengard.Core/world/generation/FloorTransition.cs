using Telengard.Core.Simulation;

namespace Telengard.Core.World.Generation;

public enum StairDirection
{
    Up,
    Down
}

public sealed record ChangeFloorCommand(StairDirection Direction) : ICommand;

public sealed record FloorChangedEvent(
    DungeonPosition From,
    DungeonPosition To,
    StairDirection Direction) : IDomainEvent;

public static class FloorTransitionResolver
{
    public static CommandResult Apply(
        GameState state,
        ChangeFloorCommand command,
        FloorLayout currentLayout,
        FloorLayout destinationLayout)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(currentLayout);
        ArgumentNullException.ThrowIfNull(destinationLayout);
        if (!state.Player.Alive) throw new InvalidOperationException("A dead player cannot change floors.");
        if (state.Combat is not null) throw new InvalidOperationException("The player cannot change floors during combat.");

        if (!Enum.IsDefined(command.Direction))
        {
            throw new ArgumentOutOfRangeException(nameof(command), command.Direction, "Unknown stair direction.");
        }

        if (!state.Expedition.Active) throw new InvalidOperationException("Floor transitions require an active expedition.");
        if (state.Inn.IsAtInn) throw new InvalidOperationException("Floor transitions are unavailable at the inn.");

        var from = state.Player.Position;
        if (from.Floor != currentLayout.Floor || currentLayout.GetTile(from) is not (DungeonTile.StairsUp or DungeonTile.StairsDown))
        {
            throw new InvalidOperationException("The player is not standing on stairs.");
        }

        var expectedTile = command.Direction is StairDirection.Up ? DungeonTile.StairsUp : DungeonTile.StairsDown;
        if (currentLayout.GetTile(from) != expectedTile)
        {
            throw new InvalidOperationException("The selected stair direction does not match the current stairs.");
        }

        var targetFloor = command.Direction is StairDirection.Up ? currentLayout.Floor - 1 : currentLayout.Floor + 1;
        if (targetFloor is < 1 or > 50 || destinationLayout.Floor != targetFloor)
        {
            throw new InvalidOperationException("The selected stairs do not lead to a valid adjacent floor.");
        }

        var to = command.Direction is StairDirection.Up
            ? destinationLayout.StairsDown
            : destinationLayout.StairsUp;
        var expedition = state.Expedition;
        var floorsVisited = expedition.FloorsVisited.Contains(to.Floor)
            ? expedition.FloorsVisited
            : [.. expedition.FloorsVisited, to.Floor];
        var nextState = state with
        {
            Player = state.Player with { Position = to },
            Expedition = expedition.Active
                ? expedition with
                {
                    DeepestFloorReached = Math.Max(expedition.DeepestFloorReached, to.Floor),
                    FloorsVisited = floorsVisited
                }
                : expedition
        };
        return new CommandResult(nextState, [new FloorChangedEvent(from, to, command.Direction)]);
    }
}
