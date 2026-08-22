using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Telengard.Core.Combat;

namespace Telengard.Core.Simulation;

public sealed record TeleportDebugCommand : ICommand
{
    public TeleportDebugCommand(DungeonPosition destination)
    {
        Destination = destination ?? throw new ArgumentNullException(nameof(destination));
    }

    public DungeonPosition Destination { get; }
}

public sealed record SetPlayerHitPointsDebugCommand(int HitPoints) : ICommand;

public sealed record SetPlayerLevelDebugCommand(int Level) : ICommand;

public sealed record GrantItemDebugCommand : ICommand
{
    public GrantItemDebugCommand(string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ItemId = itemId;
    }

    public string ItemId { get; }
}

public sealed record GrantGoldDebugCommand(int Amount) : ICommand;

public sealed record SpawnMonsterDebugCommand : ICommand
{
    public SpawnMonsterDebugCommand(string definitionId, int level, int currentHitPoints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionId);
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(currentHitPoints);
        DefinitionId = definitionId;
        Level = level;
        CurrentHitPoints = currentHitPoints;
    }

    public string DefinitionId { get; }
    public int Level { get; }
    public int CurrentHitPoints { get; }
}

public sealed record SpawnFeatureDebugCommand : ICommand
{
    public SpawnFeatureDebugCommand(string definitionId, DungeonPosition position)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionId);
        Position = position ?? throw new ArgumentNullException(nameof(position));
        DefinitionId = definitionId;
    }

    public string DefinitionId { get; }
    public DungeonPosition Position { get; }
}

public sealed record RevealMapDebugCommand : ICommand
{
    public RevealMapDebugCommand(IEnumerable<DungeonPosition> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);
        var copy = positions.ToArray();
        if (copy.Length == 0 || copy.Any(position => position is null))
        {
            throw new ArgumentException("At least one non-null map position is required.", nameof(positions));
        }

        Positions = Array.AsReadOnly(copy.Distinct().ToArray());
    }

    public IReadOnlyList<DungeonPosition> Positions { get; }
}

public sealed record DebugTeleportedEvent(DungeonPosition From, DungeonPosition To) : IDomainEvent;

public sealed record DebugHitPointsSetEvent(int PreviousHitPoints, int HitPoints) : IDomainEvent;

public sealed record DebugLevelSetEvent(int PreviousLevel, int Level) : IDomainEvent;

public sealed record DebugItemGrantedEvent(string ItemId, bool Secured) : IDomainEvent;

public sealed record DebugGoldGrantedEvent(int Amount, int CarriedGold, int SecuredGold) : IDomainEvent;

public sealed record DebugMonsterSpawnedEvent(Guid MonsterInstanceId, DungeonPosition Position) : IDomainEvent;

public sealed record DebugFeatureSpawnedEvent(
    Guid FeatureInstanceId,
    string DefinitionId,
    DungeonPosition Position) : IDomainEvent;

public sealed record DebugMapRevealedEvent(IReadOnlyList<DungeonPosition> Positions) : IDomainEvent;

public static class DeveloperDebugResolver
{
    public static CommandResult Teleport(GameState state, TeleportDebugCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        if (!state.Player.Alive)
        {
            throw new InvalidOperationException("A dead player cannot be teleported.");
        }

        var from = state.Player.Position;
        var expedition = state.Expedition;
        if (expedition.Active)
        {
            var floorsVisited = expedition.FloorsVisited.Contains(command.Destination.Floor)
                ? expedition.FloorsVisited
                : expedition.FloorsVisited.Append(command.Destination.Floor).ToArray();
            expedition = expedition with
            {
                DeepestFloorReached = Math.Max(expedition.DeepestFloorReached, command.Destination.Floor),
                FloorsVisited = floorsVisited
            };
        }

        return new CommandResult(
            state with
            {
                Player = state.Player with { Position = command.Destination },
                Expedition = expedition
            },
            [new DebugTeleportedEvent(from, command.Destination)]);
    }

    public static CommandResult SetHitPoints(GameState state, SetPlayerHitPointsDebugCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfNegative(command.HitPoints);
        if (command.HitPoints > state.Player.MaxHitPoints)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command.HitPoints),
                command.HitPoints,
                "Hit points cannot exceed the player's maximum hit points.");
        }

        return new CommandResult(
            state with { Player = state.Player with { HitPoints = command.HitPoints } },
            [new DebugHitPointsSetEvent(state.Player.HitPoints, command.HitPoints)]);
    }

    public static CommandResult SetLevel(GameState state, SetPlayerLevelDebugCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfLessThan(command.Level, 1);

        return new CommandResult(
            state with { Player = state.Player with { Level = command.Level } },
            [new DebugLevelSetEvent(state.Player.Level, command.Level)]);
    }

    public static CommandResult GrantItem(GameState state, GrantItemDebugCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (state.Expedition.Active && !state.Inn.IsAtInn)
        {
            return new CommandResult(
                state with
                {
                    Expedition = state.Expedition with
                    {
                        AcquiredItems = state.Expedition.AcquiredItems.Append(command.ItemId).ToArray()
                    }
                },
                [new DebugItemGrantedEvent(command.ItemId, false)]);
        }

        return new CommandResult(
            state with { Player = state.Player with { Inventory = state.Player.Inventory.Append(command.ItemId).ToArray() } },
            [new DebugItemGrantedEvent(command.ItemId, true)]);
    }

    public static CommandResult GrantGold(GameState state, GrantGoldDebugCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfLessThan(command.Amount, 1);

        if (state.Expedition.Active && !state.Inn.IsAtInn)
        {
            if (state.Player.CarriedGold != state.Expedition.CarriedGold)
            {
                throw new InvalidOperationException("Player and expedition carried gold must match.");
            }

            if (command.Amount > int.MaxValue - state.Expedition.CarriedGold)
            {
                throw new OverflowException("Carried gold exceeds the supported range.");
            }

            var carriedGold = state.Expedition.CarriedGold + command.Amount;
            return new CommandResult(
                state with
                {
                    Player = state.Player with { CarriedGold = carriedGold },
                    Expedition = state.Expedition with { CarriedGold = carriedGold }
                },
                [new DebugGoldGrantedEvent(command.Amount, carriedGold, state.SecuredProgress.SecuredGold)]);
        }

        if (command.Amount > int.MaxValue - state.SecuredProgress.SecuredGold)
        {
            throw new OverflowException("Secured gold exceeds the supported range.");
        }

        var securedGold = state.SecuredProgress.SecuredGold + command.Amount;
        return new CommandResult(
            state with { SecuredProgress = state.SecuredProgress with { SecuredGold = securedGold } },
            [new DebugGoldGrantedEvent(command.Amount, state.Player.CarriedGold, securedGold)]);
    }

    public static CommandResult SpawnMonster(GameState state, SpawnMonsterDebugCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        if (!state.Expedition.Active)
        {
            throw new InvalidOperationException("A debug monster requires an active expedition.");
        }

        if (!state.Player.Alive)
        {
            throw new InvalidOperationException("A dead player cannot start combat.");
        }

        if (state.Combat is not null)
        {
            throw new InvalidOperationException("Combat is already active.");
        }

        var monster = new MonsterInstance(
            CreateStableId(state, "monster", command.DefinitionId, command.Level, command.CurrentHitPoints),
            command.DefinitionId,
            command.Level,
            command.CurrentHitPoints,
            state.Player.Position);

        return new CommandResult(
            state with { Combat = CombatStateResolver.Begin(monster) },
            [new DebugMonsterSpawnedEvent(monster.InstanceId, monster.Position)]);
    }

    public static CommandResult SpawnFeature(GameState state, SpawnFeatureDebugCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        var instanceId = CreateStableId(
            state,
            "feature",
            command.DefinitionId,
            command.Position.Floor,
            command.Position.X,
            command.Position.Y,
            state.Dungeon.Features.Count);
        if (state.Dungeon.Features.Any(feature => feature.InstanceId == instanceId))
        {
            throw new InvalidOperationException("The debug feature id already exists.");
        }

        var feature = new World.Features.FeatureInstance(instanceId, command.DefinitionId, command.Position);
        return new CommandResult(
            state with { Dungeon = state.Dungeon with { Features = state.Dungeon.Features.Append(feature).ToArray() } },
            [new DebugFeatureSpawnedEvent(instanceId, command.DefinitionId, command.Position)]);
    }

    public static CommandResult RevealMap(GameState state, RevealMapDebugCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        var map = state.Legacy.PersistentMap;
        var nextMap = new PersistentMapState(map.ObservedPositions.Concat(command.Positions), map.VisitedPositions);
        return new CommandResult(
            state with { Legacy = state.Legacy with { PersistentMap = nextMap } },
            [new DebugMapRevealedEvent(command.Positions)]);
    }

    private static Guid CreateStableId(GameState state, string kind, params object[] values)
    {
        var input = string.Join(
            ":",
            state.WorldSeed,
            state.Versions.SimulationVersion,
            state.SimulationTick,
            kind,
            string.Join(
                ":",
                values.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture))));
        return new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(input))[..16]);
    }
}
