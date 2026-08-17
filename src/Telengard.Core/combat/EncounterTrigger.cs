using System.Security.Cryptography;
using System.Text;
using Telengard.Core.Rng;
using Telengard.Core.Simulation;

namespace Telengard.Core.Combat;

public sealed record EncounterSpawnOption
{
    public EncounterSpawnOption(string definitionId, int level, int currentHitPoints)
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

public sealed record EncounterTriggerConfiguration
{
    public EncounterTriggerConfiguration(
        double triggerChance,
        IEnumerable<EncounterSpawnOption>? spawnOptions = null)
    {
        if (double.IsNaN(triggerChance) || triggerChance is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(triggerChance), triggerChance, "Trigger chance must be between zero and one.");
        }

        TriggerChance = triggerChance;
        SpawnOptions = Array.AsReadOnly((spawnOptions ?? []).Select(option =>
        {
            ArgumentNullException.ThrowIfNull(option);
            return option;
        }).ToArray());
    }

    public double TriggerChance { get; }
    public IReadOnlyList<EncounterSpawnOption> SpawnOptions { get; }
}

public sealed record EncounterStartedEvent(MonsterInstance Monster) : IDomainEvent;

public static class EncounterTriggerResolver
{
    public static CommandResult Evaluate(
        GameState state,
        DungeonPosition position,
        EncounterTriggerConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(configuration);

        if (!state.Expedition.Active) throw new InvalidOperationException("An encounter requires an active expedition.");
        if (!state.Player.Alive) throw new InvalidOperationException("A dead player cannot start an encounter.");
        if (state.Player.Position != position) throw new InvalidOperationException("The encounter position must match the player position.");
        if (state.Combat is not null) throw new InvalidOperationException("A combat encounter is already active.");
        if (configuration.SpawnOptions.Count == 0 || configuration.TriggerChance == 0)
        {
            return new CommandResult(state);
        }

        var stream = new DeterministicRng(state.WorldSeed, state.Versions.GeneratorVersion)
            .CreateStream(
                "encounter",
                $"expedition:{state.Expedition.ExpeditionId?.ToString() ?? "none"}",
                $"tick:{state.SimulationTick}",
                $"floor:{position.Floor}",
                $"x:{position.X}",
                $"y:{position.Y}");
        if (stream.NextDouble() >= configuration.TriggerChance)
        {
            return new CommandResult(state);
        }

        var optionIndex = stream.NextInt(0, configuration.SpawnOptions.Count);
        var option = configuration.SpawnOptions[optionIndex];
        var monster = new MonsterInstance(
            CreateInstanceId(state, position, option, optionIndex),
            option.DefinitionId,
            option.Level,
            option.CurrentHitPoints,
            position);

        return new CommandResult(
            state with { Combat = CombatStateResolver.Begin(monster) },
            [new EncounterStartedEvent(monster)]);
    }

    private static Guid CreateInstanceId(
        GameState state,
        DungeonPosition position,
        EncounterSpawnOption option,
        int optionIndex)
    {
        var input = $"{state.WorldSeed}:{state.Versions.GeneratorVersion}:{state.Expedition.ExpeditionId}:{state.SimulationTick}:{position.Floor}:{position.X}:{position.Y}:{option.DefinitionId}:{optionIndex}";
        return new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(input))[..16]);
    }
}
