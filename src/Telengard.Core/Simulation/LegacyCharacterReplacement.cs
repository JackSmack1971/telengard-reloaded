namespace Telengard.Core.Simulation;

public sealed record ReplaceLegacyCharacterCommand(
    Guid NewPlayerId,
    CharacterCreationRequest Request) : ICommand;

public sealed record LegacyCharacterReplacedEvent(
    Guid PreviousPlayerId,
    Guid NewPlayerId) : IDomainEvent;

public static class LegacyCharacterReplacementResolver
{
    public static CommandResult Resolve(
        GameState state,
        ReplaceLegacyCharacterCommand command,
        ICharacterCreationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(provider);

        if (state.CurrentMode != GameMode.Legacy)
        {
            throw new InvalidOperationException("Legacy character replacement requires Legacy mode.");
        }

        if (state.Player.Alive)
        {
            throw new InvalidOperationException("Legacy character replacement requires a dead character.");
        }

        if (state.Legacy.PreviousHeroes.All(hero => hero.HeroId != state.Player.Id))
        {
            throw new InvalidOperationException("The dead character is not recorded in Legacy history.");
        }

        if (state.Legacy.PreviousHeroes.Any(hero => hero.HeroId == command.NewPlayerId))
        {
            throw new ArgumentException(
                "The replacement character identity must not reuse Legacy history.",
                nameof(command));
        }

        if (state.Player.HitPoints != 0 ||
            state.Player.CarriedGold != 0 ||
            state.Player.Inventory.Count != 0 ||
            state.Player.EquipmentSlots.Any(slot => slot.ItemInstanceId.HasValue) ||
            state.Expedition.Active ||
            state.Expedition.CarriedGold != 0 ||
            state.Expedition.AcquiredItems.Count != 0 ||
            !state.Inn.IsAtInn ||
            state.Combat is not null)
        {
            throw new InvalidOperationException(
                "Legacy character replacement requires an inactive expedition at the inn outside combat.");
        }

        var creation = CharacterCreationResolver.Resolve(
            state with { Player = new PlayerState { Id = command.NewPlayerId } },
            new CreateCharacterCommand(command.Request),
            provider);
        var player = creation.State.Player;
        ValidateNewCharacter(player);
        if (player.Id != command.NewPlayerId)
        {
            throw new ArgumentException(
                "The character creation provider must preserve the requested identity.",
                nameof(command));
        }

        var next = state with
        {
            Player = player,
            Expedition = new ExpeditionState(),
            Inn = state.Inn with { IsAtInn = true },
            Combat = null
        };

        return new CommandResult(
            next,
            creation.Events.Append(new LegacyCharacterReplacedEvent(state.Player.Id, player.Id)));
    }

    private static void ValidateNewCharacter(PlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!player.Alive)
        {
            throw new ArgumentException("The replacement character must be alive.", nameof(player));
        }

        if (player.Attributes is null ||
            player.CarriedGold != 0 ||
            player.Position != new DungeonPosition(1, 0, 0) ||
            player.Inventory.Count != 0 ||
            player.EquipmentSlots.Count != 0 ||
            player.Talents.Count != 0 ||
            player.Spells.Count != 0 ||
            player.Injuries.Count != 0 ||
            player.TemporaryEffects.Count != 0 ||
            player.Level != 1 ||
            player.Experience != 0 ||
            player.HitPoints < 0 ||
            player.MaxHitPoints < 0 ||
            player.HitPoints > player.MaxHitPoints ||
            player.SpellPower < 0 ||
            player.MaxSpellPower < 0 ||
            player.SpellPower > player.MaxSpellPower)
        {
            throw new ArgumentException(
                "The replacement character contains non-initial or invalid state.",
                nameof(player));
        }
    }
}
