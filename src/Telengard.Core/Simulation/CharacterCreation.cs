namespace Telengard.Core.Simulation;

public enum CharacterCreationMode
{
    Rolled,
    PointAllocation,
    DailySeed
}

public interface ICharacterCreationInput;

public sealed record CharacterCreationRequest(
    CharacterCreationMode Mode,
    ICharacterCreationInput? Input = null);

public sealed record CreateCharacterCommand(CharacterCreationRequest Request) : ICommand;

public sealed record CharacterCreationResult
{
    public CharacterCreationResult(PlayerState player)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
    }

    public PlayerState Player { get; }
}

public interface ICharacterCreationProvider
{
    CharacterCreationMode Mode { get; }

    CharacterCreationResult Create(GameState state, CharacterCreationRequest request);
}

public sealed record CharacterCreatedEvent(
    Guid PlayerId,
    CharacterCreationMode Mode) : IDomainEvent;

public static class CharacterCreationResolver
{
    public static CommandResult Resolve(
        GameState state,
        CreateCharacterCommand command,
        ICharacterCreationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(command.Request);

        if (!Enum.IsDefined(command.Request.Mode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(command.Request.Mode),
                command.Request.Mode,
                "Unknown character creation mode.");
        }

        if (!Enum.IsDefined(provider.Mode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(provider.Mode),
                provider.Mode,
                "Unknown character creation provider mode.");
        }

        if (provider.Mode != command.Request.Mode)
        {
            throw new InvalidOperationException("The character creation provider does not match the requested mode.");
        }

        var result = provider.Create(state, command.Request)
            ?? throw new InvalidOperationException("The character creation provider returned no result.");

        var next = state with { Player = result.Player };
        return new CommandResult(
            next,
            [new CharacterCreatedEvent(result.Player.Id, command.Request.Mode)]);
    }
}
