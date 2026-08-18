using Telengard.Core.Items;
using Telengard.Core.Simulation;

namespace Telengard.Core.Combat;

public sealed record PlayerDeathCommand : ICommand;

public sealed record PlayerDiedEvent(
    Guid? ExpeditionId,
    DungeonPosition Position) : IDomainEvent;

public sealed record ExpeditionFailedEvent(
    Guid? ExpeditionId,
    int DeepestFloorReached) : IDomainEvent;

public static class PlayerDeathResolver
{
    public static CommandResult Resolve(GameState state, PlayerDeathCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (!state.Expedition.Active)
        {
            throw new InvalidOperationException("Player death requires an active expedition.");
        }

        if (!state.Player.Alive)
        {
            throw new InvalidOperationException("The player is already dead.");
        }

        if (state.Player.HitPoints > 0)
        {
            throw new InvalidOperationException("The player cannot die while hit points remain.");
        }

        var losesExpeditionTreasure = state.CurrentMode is GameMode.Classic or GameMode.Legacy or GameMode.Adventure;
        var next = state with
        {
            Player = state.CurrentMode switch
            {
                GameMode.Classic => DeleteClassicCharacter(),
                GameMode.Legacy => ResolveLegacyDeath(state.Player),
                GameMode.Adventure => ResolveAdventureDeath(state.Player),
                _ => throw new ArgumentOutOfRangeException(nameof(state.CurrentMode), state.CurrentMode, "Unknown game mode.")
            },
            Expedition = state.Expedition with
            {
                Active = false,
                CarriedGold = losesExpeditionTreasure
                    ? 0
                    : state.Expedition.CarriedGold,
                AcquiredItems = losesExpeditionTreasure
                    ? Array.Empty<string>()
                    : state.Expedition.AcquiredItems
            },
            Inn = state.Inn with { IsAtInn = true },
            Combat = null
        };

        return new CommandResult(
            next,
            [
                new PlayerDiedEvent(state.Expedition.ExpeditionId, state.Player.Position),
                new ExpeditionFailedEvent(state.Expedition.ExpeditionId, state.Expedition.DeepestFloorReached)
            ]);
    }

    private static PlayerState DeleteClassicCharacter() => new()
    {
        Level = 0,
        Alive = false
    };

    private static PlayerState ResolveLegacyDeath(PlayerState player) => player with
    {
        Alive = false,
        HitPoints = 0,
        CarriedGold = 0,
        Inventory = [],
        EquipmentSlots = player.EquipmentSlots
            .Select(slot => new EquipmentSlotState(slot.SlotId))
            .ToArray()
    };

    private static PlayerState ResolveAdventureDeath(PlayerState player) => player with
    {
        Alive = true,
        HitPoints = player.MaxHitPoints,
        CarriedGold = 0
    };
}
