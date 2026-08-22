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

public sealed record PointAllocationCharacterCreationInput : ICharacterCreationInput
{
    public PointAllocationCharacterCreationInput(PlayerAttributes attributes)
    {
        Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
    }

    public PlayerAttributes Attributes { get; }
}

public sealed record PointAllocationCharacterCreationConfiguration
{
    public PointAllocationCharacterCreationConfiguration(
        int pointBudget,
        int minimumAttribute,
        int maximumAttribute)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pointBudget);
        if (minimumAttribute > maximumAttribute)
        {
            throw new ArgumentException("The minimum attribute must not exceed the maximum attribute.");
        }

        PointBudget = pointBudget;
        MinimumAttribute = minimumAttribute;
        MaximumAttribute = maximumAttribute;
    }

    public int PointBudget { get; }
    public int MinimumAttribute { get; }
    public int MaximumAttribute { get; }
}

public sealed class PointAllocationCharacterCreationProvider : ICharacterCreationProvider
{
    private readonly PointAllocationCharacterCreationConfiguration _configuration;

    public PointAllocationCharacterCreationProvider(
        PointAllocationCharacterCreationConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public CharacterCreationMode Mode => CharacterCreationMode.PointAllocation;

    public CharacterCreationResult Create(GameState state, CharacterCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(request);

        if (request.Mode != Mode)
        {
            throw new InvalidOperationException("The point-allocation provider requires the point-allocation creation mode.");
        }

        if (request.Input is not PointAllocationCharacterCreationInput input)
        {
            throw new ArgumentException(
                "Point allocation requires six attribute values.",
                nameof(request));
        }

        var attributes = input.Attributes;
        ValidateAttribute(attributes.Strength, nameof(attributes.Strength));
        ValidateAttribute(attributes.Intelligence, nameof(attributes.Intelligence));
        ValidateAttribute(attributes.Wisdom, nameof(attributes.Wisdom));
        ValidateAttribute(attributes.Constitution, nameof(attributes.Constitution));
        ValidateAttribute(attributes.Dexterity, nameof(attributes.Dexterity));
        ValidateAttribute(attributes.Charisma, nameof(attributes.Charisma));

        var total = (long)attributes.Strength
            + attributes.Intelligence
            + attributes.Wisdom
            + attributes.Constitution
            + attributes.Dexterity
            + attributes.Charisma;
        if (total != _configuration.PointBudget)
        {
            throw new ArgumentException(
                $"The six attribute allocations must total {_configuration.PointBudget} points.",
                nameof(request));
        }

        return new CharacterCreationResult(state.Player with { Attributes = attributes });
    }

    private void ValidateAttribute(int value, string parameterName)
    {
        if (value < _configuration.MinimumAttribute || value > _configuration.MaximumAttribute)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Attribute values must be between {_configuration.MinimumAttribute} and {_configuration.MaximumAttribute}.");
        }
    }
}

public sealed record DailySeedCharacterCreationInput : ICharacterCreationInput
{
    public DailySeedCharacterCreationInput(string dailySeed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dailySeed);
        DailySeed = dailySeed;
    }

    public string DailySeed { get; }
}

public sealed record DailySeedCharacterCreationConfiguration
{
    public DailySeedCharacterCreationConfiguration(
        string policyVersion,
        int minimumAttribute,
        int maximumAttribute)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyVersion);
        if (minimumAttribute > maximumAttribute)
        {
            throw new ArgumentException("The minimum attribute must not exceed the maximum attribute.");
        }

        PolicyVersion = policyVersion;
        MinimumAttribute = minimumAttribute;
        MaximumAttribute = maximumAttribute;
    }

    public string PolicyVersion { get; }
    public int MinimumAttribute { get; }
    public int MaximumAttribute { get; }
}

public sealed class DailySeedCharacterCreationProvider : ICharacterCreationProvider
{
    private readonly DailySeedCharacterCreationConfiguration _configuration;

    public DailySeedCharacterCreationProvider(DailySeedCharacterCreationConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public CharacterCreationMode Mode => CharacterCreationMode.DailySeed;

    public CharacterCreationResult Create(GameState state, CharacterCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(request);

        if (request.Mode != Mode)
        {
            throw new InvalidOperationException("The daily-seed provider requires the daily-seed creation mode.");
        }

        if (request.Input is not DailySeedCharacterCreationInput input)
        {
            throw new ArgumentException(
                "Daily-seed creation requires a stable daily-seed value.",
                nameof(request));
        }

        var stream = new DeterministicRng(0, _configuration.PolicyVersion)
            .CreateStream("character-creation", "daily-seed", input.DailySeed);
        var attributes = new PlayerAttributes(
            RollAttribute(stream),
            RollAttribute(stream),
            RollAttribute(stream),
            RollAttribute(stream),
            RollAttribute(stream),
            RollAttribute(stream));

        return new CharacterCreationResult(state.Player with { Attributes = attributes });
    }

    private int RollAttribute(DeterministicRngStream stream)
    {
        return checked((int)stream.NextLong(
            _configuration.MinimumAttribute,
            (long)_configuration.MaximumAttribute + 1));
    }
}

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
