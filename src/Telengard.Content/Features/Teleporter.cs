using Telengard.Core.Knowledge;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;

namespace Telengard.Content;

public sealed record TeleporterNode
{
    public TeleporterNode(
        string nodeId,
        DungeonPosition position,
        string networkId,
        string destinationRule)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentNullException.ThrowIfNull(position);
        ArgumentException.ThrowIfNullOrWhiteSpace(networkId);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRule);

        NodeId = nodeId;
        Position = position;
        NetworkId = networkId;
        DestinationRule = destinationRule;
    }

    public string NodeId { get; }
    public DungeonPosition Position { get; }
    public string NetworkId { get; }
    public string DestinationRule { get; }
}

public static class TeleporterResolver
{
    public static CommandResult Resolve(
        GameState state,
        ActivateFeatureCommand command,
        FeatureDefinition definition,
        DungeonPosition destination,
        FeatureOutcomeSelectionContext? context = null)
        => ResolveCore(state, command, definition, null, destination, context);

    public static CommandResult Resolve(
        GameState state,
        ActivateFeatureCommand command,
        FeatureDefinition definition,
        TeleporterNode node,
        DungeonPosition destination,
        FeatureOutcomeSelectionContext? context = null)
        => ResolveCore(state, command, definition, node, destination, context);

    private static CommandResult ResolveCore(
        GameState state,
        ActivateFeatureCommand command,
        FeatureDefinition definition,
        TeleporterNode? node,
        DungeonPosition destination,
        FeatureOutcomeSelectionContext? context)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(destination);

        if (definition.Type != FeatureType.Teleporter)
        {
            throw new ArgumentException("The feature definition must describe a teleporter.", nameof(definition));
        }

        var feature = state.Dungeon.Features.SingleOrDefault(candidate => candidate.InstanceId == command.FeatureId)
            ?? throw new InvalidOperationException("The requested teleporter does not exist in the current dungeon state.");
        if (!string.Equals(feature.DefinitionId, definition.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The teleporter definition does not match the dungeon feature.");
        }

        if (node is not null && node.Position != feature.Position)
        {
            throw new InvalidOperationException("The teleporter node does not match the dungeon feature position.");
        }

        var outcome = FeatureOutcomeEngine.Select(
            definition,
            context ?? new FeatureOutcomeSelectionContext(),
            state.WorldSeed,
            state.Versions.ContentVersion,
            feature.InstanceId,
            feature.Position,
            feature.ActivationCount);

        var activation = FeatureActivationResolver.ActivateTeleporter(
            state,
            command,
            destination,
            new FeatureOutcomeResolution(outcome.Effects, outcome.Observations));

        if (node is null)
        {
            return activation;
        }

        var mapping = TeleporterMappingResolver.Add(
            activation.State,
            new AddTeleporterMappingCommand(
                node.NetworkId,
                node.NodeId,
                node.Position,
                destination));
        return new CommandResult(
            mapping.State,
            activation.Events.Concat(mapping.Events));
    }
}
