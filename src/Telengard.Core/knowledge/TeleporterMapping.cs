using Telengard.Core.Simulation;

namespace Telengard.Core.Knowledge;

public enum TeleporterMappingStatus
{
    Unknown,
    Observed,
    Mapped
}

public sealed record TeleporterMapping
{
    public TeleporterMapping(
        string networkId,
        string nodeId,
        DungeonPosition source,
        DungeonPosition destination,
        TeleporterMappingStatus status = TeleporterMappingStatus.Observed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(networkId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (status is TeleporterMappingStatus.Unknown)
        {
            throw new ArgumentException("A stored teleporter mapping must be observed or mapped.", nameof(status));
        }

        NetworkId = networkId;
        NodeId = nodeId;
        Source = source;
        Destination = destination;
        Status = status;
    }

    public string NetworkId { get; }
    public string NodeId { get; }
    public DungeonPosition Source { get; }
    public DungeonPosition Destination { get; }
    public TeleporterMappingStatus Status { get; init; }

    public TeleporterMapping Confirm() => this with { Status = TeleporterMappingStatus.Mapped };
}

public sealed record AddTeleporterMappingCommand : ICommand
{
    public AddTeleporterMappingCommand(
        string networkId,
        string nodeId,
        DungeonPosition source,
        DungeonPosition destination)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(networkId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        NetworkId = networkId;
        NodeId = nodeId;
        Source = source;
        Destination = destination;
    }

    public string NetworkId { get; }
    public string NodeId { get; }
    public DungeonPosition Source { get; }
    public DungeonPosition Destination { get; }
}

public sealed record TeleporterMappingObservedEvent(
    string NetworkId,
    string NodeId,
    DungeonPosition Source,
    DungeonPosition Destination,
    TeleporterMappingStatus Status) : IDomainEvent;

public static class TeleporterMappingResolver
{
    public static CommandResult Add(GameState state, AddTeleporterMappingCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (!state.Expedition.Active)
        {
            throw new InvalidOperationException("Teleporter mapping requires an active expedition.");
        }

        if (!state.Player.Alive)
        {
            throw new InvalidOperationException("A dead player cannot add a teleporter mapping.");
        }

        var existing = state.Knowledge.TeleporterMappings.SingleOrDefault(mapping =>
            string.Equals(mapping.NetworkId, command.NetworkId, StringComparison.Ordinal) &&
            string.Equals(mapping.NodeId, command.NodeId, StringComparison.Ordinal) &&
            mapping.Source == command.Source &&
            mapping.Destination == command.Destination);

        TeleporterMapping updated;
        if (existing is null)
        {
            updated = new TeleporterMapping(
                command.NetworkId,
                command.NodeId,
                command.Source,
                command.Destination);
        }
        else if (existing.Status == TeleporterMappingStatus.Observed)
        {
            updated = existing.Confirm();
        }
        else
        {
            return new CommandResult(state);
        }

        var mappings = existing is null
            ? state.Knowledge.TeleporterMappings.Append(updated)
            : state.Knowledge.TeleporterMappings.Select(mapping =>
                ReferenceEquals(mapping, existing) ? updated : mapping);
        var nextState = state with
        {
            Knowledge = new KnowledgeState(state.Knowledge.Entries, mappings)
        };

        return new CommandResult(
            nextState,
            [new TeleporterMappingObservedEvent(
                updated.NetworkId,
                updated.NodeId,
                updated.Source,
                updated.Destination,
                updated.Status)]);
    }

    public static TeleporterMappingStatus GetStatus(
        KnowledgeState knowledge,
        string networkId,
        string nodeId,
        DungeonPosition source,
        DungeonPosition destination)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentException.ThrowIfNullOrWhiteSpace(networkId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        return knowledge.TeleporterMappings
            .SingleOrDefault(mapping =>
                string.Equals(mapping.NetworkId, networkId, StringComparison.Ordinal) &&
                string.Equals(mapping.NodeId, nodeId, StringComparison.Ordinal) &&
                mapping.Source == source &&
                mapping.Destination == destination)
            ?.Status ?? TeleporterMappingStatus.Unknown;
    }
}
