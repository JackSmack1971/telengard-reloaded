using Telengard.Core.Rng;

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

public sealed record RolledAttributeRange
{
    public RolledAttributeRange(int minimumInclusive, int maximumInclusive)
    {
        if (minimumInclusive > maximumInclusive)
        {
            throw new ArgumentException("The minimum attribute must not exceed the maximum attribute.");
        }

        MinimumInclusive = minimumInclusive;
        MaximumInclusive = maximumInclusive;
    }

    public int MinimumInclusive { get; }
    public int MaximumInclusive { get; }
}

public sealed record RolledCharacterCreationConfiguration
{
    private readonly IReadOnlyList<RolledAttributeRange> _attributeRanges;

    public RolledCharacterCreationConfiguration(
        string policyVersion,
        IEnumerable<RolledAttributeRange> attributeRanges)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyVersion);
        ArgumentNullException.ThrowIfNull(attributeRanges);

        var ranges = attributeRanges.ToArray();
        if (ranges.Length != 6)
        {
            throw new ArgumentException("Exactly six attribute ranges are required.", nameof(attributeRanges));
        }

        if (ranges.Any(range => range is null))
        {
            throw new ArgumentException("Attribute ranges cannot contain null values.", nameof(attributeRanges));
        }

        PolicyVersion = policyVersion;
        _attributeRanges = Array.AsReadOnly(ranges);
    }

    public string PolicyVersion { get; }
    public IReadOnlyList<RolledAttributeRange> AttributeRanges => _attributeRanges;
}

public sealed class RolledCharacterCreationProvider : ICharacterCreationProvider
{
    private readonly RolledCharacterCreationConfiguration _configuration;

    public RolledCharacterCreationProvider(RolledCharacterCreationConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public CharacterCreationMode Mode => CharacterCreationMode.Rolled;

    public CharacterCreationResult Create(GameState state, CharacterCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(request);

        if (request.Mode != Mode)
        {
            throw new InvalidOperationException("The rolled provider requires the rolled creation mode.");
        }

        var stream = new DeterministicRng(state.WorldSeed, state.Versions.SimulationVersion)
            .CreateStream(
                "character-creation",
                "mode:rolled",
                $"player:{state.Player.Id}",
                $"policy:{_configuration.PolicyVersion}");
        var attributes = new PlayerAttributes(
            RollAttribute(stream, _configuration.AttributeRanges[0]),
            RollAttribute(stream, _configuration.AttributeRanges[1]),
            RollAttribute(stream, _configuration.AttributeRanges[2]),
            RollAttribute(stream, _configuration.AttributeRanges[3]),
            RollAttribute(stream, _configuration.AttributeRanges[4]),
            RollAttribute(stream, _configuration.AttributeRanges[5]));

        return new CharacterCreationResult(state.Player with { Attributes = attributes });
    }

    private static int RollAttribute(DeterministicRngStream stream, RolledAttributeRange range) =>
        (int)stream.NextLong(range.MinimumInclusive, (long)range.MaximumInclusive + 1);
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
