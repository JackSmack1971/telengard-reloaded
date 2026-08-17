using Telengard.Core.Simulation;

namespace Telengard.Core.Meta;

public sealed record SuspendExpeditionCommand : ICommand;

public sealed record GameSuspendedEvent(Guid? ExpeditionId, DungeonPosition Position) : IDomainEvent;

public static class ExpeditionSuspensionResolver
{
    public static CommandResult Suspend(GameState state, SuspendExpeditionCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (!state.Expedition.Active || state.Inn.IsAtInn)
        {
            throw new InvalidOperationException("Only an active dungeon expedition can be suspended.");
        }

        if (!state.Player.Alive)
        {
            throw new InvalidOperationException("A dead expedition cannot be suspended.");
        }

        return new CommandResult(
            state,
            [new GameSuspendedEvent(state.Expedition.ExpeditionId, state.Player.Position)]);
    }
}
