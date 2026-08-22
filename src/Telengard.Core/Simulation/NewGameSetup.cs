namespace Telengard.Core.Simulation;

public sealed record NewGameSetupRequest(
    long? WorldSeed,
    GameMode Mode,
    CharacterCreationResult Character);

public sealed record NewGameCreatedEvent(
    Guid PlayerId,
    GameMode Mode,
    long WorldSeed) : IDomainEvent;

public static class NewGameSetupResolver
{
    public static CommandResult Create(
        NewGameSetupRequest request,
        GameVersions? versions = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.WorldSeed.HasValue)
        {
            throw new ArgumentException("A world seed must be supplied.", nameof(request));
        }

        if (!Enum.IsDefined(request.Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(request.Mode), request.Mode, "Unknown game mode.");
        }

        ArgumentNullException.ThrowIfNull(request.Character);
        if (!request.Character.Player.Alive)
        {
            throw new InvalidOperationException("A new game requires a living character.");
        }

        var player = request.Character.Player;
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
                "The character result contains non-initial or invalid state.",
                nameof(request));
        }

        var state = GameState.Create(
            request.WorldSeed.Value,
            versions,
            request.Mode,
            player.Id) with
        {
            Player = player
        };

        return new CommandResult(
            state,
            [new NewGameCreatedEvent(state.Player.Id, state.CurrentMode, state.WorldSeed)]);
    }
}
