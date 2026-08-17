using Telengard.Core.Combat;
using Telengard.Core.Simulation;
using Telengard.Core.World.Visibility;
using System.Security.Cryptography;
using System.Text;

namespace Telengard.Core.World.Generation;

public enum MovementDirection
{
    North,
    South,
    East,
    West
}

public sealed record EnterDungeonCommand : ICommand;
public sealed record MoveCommand(MovementDirection Direction) : ICommand;
public sealed record LeaveDungeonCommand : ICommand;

public sealed record DungeonEnteredEvent(DungeonPosition Position) : IDomainEvent;
public sealed record ExpeditionStartedEvent(Guid ExpeditionId, int StartingFloor) : IDomainEvent;
public sealed record PlayerMovedEvent(DungeonPosition From, DungeonPosition To) : IDomainEvent;
public sealed record DungeonLeftEvent(DungeonPosition Position) : IDomainEvent;
public sealed record GoldSecuredEvent(int Amount, int SecuredGold) : IDomainEvent;
public sealed record ExpeditionSucceededEvent(Guid? ExpeditionId, int DeepestFloorReached) : IDomainEvent;

public static class DungeonWalkingResolver
{
    public static CommandResult Enter(GameState state, EnterDungeonCommand command, FloorLayout layout)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(layout);
        if (layout.Floor != 1) throw new InvalidOperationException("Dungeon entry starts on floor 1.");
        if (state.Expedition.Active) throw new InvalidOperationException("An expedition is already active.");
        if (!state.Player.Alive) throw new InvalidOperationException("A dead player cannot start an expedition.");
        if (!state.Inn.IsAtInn) throw new InvalidOperationException("The player must be at the inn to start an expedition.");

        var position = layout.StairsUp;
        var expeditionId = CreateExpeditionId(state);
        var expedition = new ExpeditionState
        {
            ExpeditionId = expeditionId,
            StartingFloor = position.Floor,
            DeepestFloorReached = position.Floor,
            StartSimulationTick = state.SimulationTick,
            CarriedGold = state.Player.CarriedGold,
            FloorsVisited = [position.Floor],
            Active = true
        };
        var next = state with
        {
            Player = state.Player with { Position = position },
            Inn = state.Inn with { IsAtInn = false },
            Expedition = expedition
        };
        return new CommandResult(Discover(next, layout, position),
            [new DungeonEnteredEvent(position), new ExpeditionStartedEvent(expeditionId, position.Floor)]);
    }

    public static CommandResult Move(
        GameState state,
        MoveCommand command,
        FloorLayout layout,
        EncounterTriggerConfiguration? encounterConfiguration = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(layout);

        if (!state.Player.Alive) throw new InvalidOperationException("A dead player cannot move.");
        if (state.Combat is not null) throw new InvalidOperationException("Movement is unavailable during combat.");
        var from = state.Player.Position;
        if (from.Floor != layout.Floor) throw new InvalidOperationException("The player is on a different floor.");

        var to = command.Direction switch
        {
            MovementDirection.North => new DungeonPosition(from.Floor, from.X, from.Y - 1),
            MovementDirection.South => new DungeonPosition(from.Floor, from.X, from.Y + 1),
            MovementDirection.East => new DungeonPosition(from.Floor, from.X + 1, from.Y),
            MovementDirection.West => new DungeonPosition(from.Floor, from.X - 1, from.Y),
            _ => throw new ArgumentOutOfRangeException(nameof(command))
        };

        if (to.X < 0 || to.X >= layout.Width || to.Y < 0 || to.Y >= layout.Height || !layout.IsWalkable(to))
            throw new InvalidOperationException("The destination is not traversable.");

        var next = Discover(state with { Player = state.Player with { Position = to } }, layout, to);
        var events = new List<IDomainEvent> { new PlayerMovedEvent(from, to) };
        if (encounterConfiguration is not null)
        {
            var encounterResult = EncounterTriggerResolver.Evaluate(next, to, encounterConfiguration);
            next = encounterResult.State;
            events.AddRange(encounterResult.Events);
        }

        return new CommandResult(next, events);
    }

    public static CommandResult Leave(GameState state, LeaveDungeonCommand command, FloorLayout layout)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(layout);
        if (!state.Expedition.Active) throw new InvalidOperationException("No expedition is active.");
        if (state.Combat is not null) throw new InvalidOperationException("The player cannot leave during combat.");
        if (state.Player.Position != layout.StairsDown || layout.Floor != 1)
            throw new InvalidOperationException("The player must be at the floor 1 entrance to leave.");

        if (state.Player.CarriedGold != state.Expedition.CarriedGold)
            throw new InvalidOperationException("Player and expedition carried gold must match.");
        if (state.Player.CarriedGold < 0 || state.SecuredProgress.SecuredGold < 0)
            throw new InvalidOperationException("Gold cannot be negative.");
        if (state.Player.CarriedGold > int.MaxValue - state.SecuredProgress.SecuredGold)
            throw new OverflowException("Secured gold exceeds the supported range.");

        var securedGold = state.SecuredProgress.SecuredGold + state.Player.CarriedGold;
        var next = state with
        {
            Inn = state.Inn with { IsAtInn = true },
            Player = state.Player with { CarriedGold = 0 },
            Expedition = state.Expedition with { CarriedGold = 0, Active = false },
            SecuredProgress = state.SecuredProgress with { SecuredGold = securedGold }
        };
        var events = state.Player.CarriedGold > 0
            ? new IDomainEvent[]
            {
                new DungeonLeftEvent(state.Player.Position),
                new GoldSecuredEvent(state.Player.CarriedGold, securedGold),
                new ExpeditionSucceededEvent(state.Expedition.ExpeditionId, state.Expedition.DeepestFloorReached)
            }
            :
            [
                new DungeonLeftEvent(state.Player.Position),
                new ExpeditionSucceededEvent(state.Expedition.ExpeditionId, state.Expedition.DeepestFloorReached)
            ];
        return new CommandResult(next, events);
    }

    private static GameState Discover(GameState state, FloorLayout layout, DungeonPosition position)
    {
        var known = state.Legacy.PersistentMap;
        var currentFloor = FogOfWarMap.Create(
            layout,
            known.ObservedPositions.Where(candidate => candidate.Floor == layout.Floor),
            known.VisitedPositions.Where(candidate => candidate.Floor == layout.Floor));
        var visible = currentFloor.Resolve(position).CurrentlyVisiblePositions;
        var updated = currentFloor.Observe(visible).Visit(position).ToPersistentState();
        return state with
        {
            Legacy = state.Legacy with
            {
                PersistentMap = new PersistentMapState(
                    known.ObservedPositions.Where(candidate => candidate.Floor != layout.Floor).Concat(updated.ObservedPositions),
                    known.VisitedPositions.Where(candidate => candidate.Floor != layout.Floor).Concat(updated.VisitedPositions))
            }
        };
    }

    private static Guid CreateExpeditionId(GameState state)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{state.WorldSeed}:{state.SimulationTick}:{state.Player.Id}"));
        return new Guid(bytes[..16]);
    }
}
