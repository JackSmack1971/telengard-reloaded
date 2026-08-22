using Telengard.Core.Simulation;
using Telengard.TestHarness;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class DeveloperDebugTests
{
    [Fact]
    public void Script_commands_use_simulation_boundaries_and_emit_stable_events()
    {
        var result = new DebugScriptSession(GameState.Create(1234)).Run(
        [
            "enter",
            "give gold 7",
            "give item amulet",
            "teleport 2 4 5",
            "spawn feature fountain 2 4 5",
            "reveal tile 2 4 5",
            "reveal floor 2",
            "set level 3",
            "spawn monster rat 1 1",
            "set hp 0",
            "trigger death"
        ]);

        Assert.False(result.HadErrors);
        Assert.Contains("\"type\":\"DebugGoldGrantedEvent\"", result.Transcript);
        Assert.Contains("\"type\":\"DebugTeleportedEvent\"", result.Transcript);
        Assert.Contains("\"type\":\"DebugFeatureSpawnedEvent\"", result.Transcript);
        Assert.Contains("\"type\":\"DebugMonsterSpawnedEvent\"", result.Transcript);
        Assert.Contains("\"type\":\"PlayerDiedEvent\"", result.Transcript);
        Assert.False(result.FinalState.Expedition.Active);
        Assert.True(result.FinalState.Inn.IsAtInn);
        Assert.Equal(0, result.FinalState.Expedition.CarriedGold);
        Assert.Single(result.FinalState.Dungeon.Features);
    }

    [Fact]
    public void Deterministic_script_replay_and_save_load_are_equal()
    {
        string[] commands =
        [
            "enter",
            "set danger 0",
            "give gold 3",
            "give item scroll",
            "teleport 2 4 5",
            "save",
            "teleport 3 6 7",
            "load",
            "inspect rng loot",
            "dump game state",
            "dump knowledge"
        ];

        var first = new DebugScriptSession(GameState.Create(9876)).Run(commands);
        var second = new DebugScriptSession(GameState.Create(9876)).Run(commands);

        Assert.False(first.HadErrors);
        Assert.Equal(first.FinalSave, second.FinalSave);
        Assert.Equal(first.Transcript, second.Transcript);
        Assert.Equal(2, first.FinalState.Player.Position.Floor);
        Assert.Equal(3, first.FinalState.Player.CarriedGold);
        Assert.Equal(["scroll"], first.FinalState.Expedition.AcquiredItems);
    }

    [Fact]
    public void Invalid_script_input_is_reported_without_mutating_state()
    {
        var initial = GameState.Create(42);
        var result = new DebugScriptSession(initial).Run(
        [
            "unknown command",
            "set hp 1",
            "set danger 0 extra"
        ]);

        Assert.True(result.HadErrors);
        Assert.Contains("\"ok\":false", result.Transcript);
        Assert.Equal(initial, result.FinalState);
    }

    [Fact]
    public void Debug_commands_validate_constructor_and_state_boundaries()
    {
        Assert.Throws<ArgumentNullException>(() => new TeleportDebugCommand(null!));
        Assert.Throws<ArgumentException>(() => new GrantItemDebugCommand(" "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpawnMonsterDebugCommand("rat", 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpawnMonsterDebugCommand("rat", 1, -1));
        Assert.Throws<ArgumentException>(() => new RevealMapDebugCommand([]));
        Assert.Throws<ArgumentNullException>(() => DeveloperDebugResolver.SetLevel(state: GameState.Create(1), command: null!));
        Assert.Throws<ArgumentNullException>(() => DeveloperDebugResolver.GrantGold(state: GameState.Create(1), command: null!));

        var state = GameState.Create(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => DeveloperDebugResolver.SetHitPoints(
            state,
            new SetPlayerHitPointsDebugCommand(1)));
        Assert.Throws<InvalidOperationException>(() => DeveloperDebugResolver.SpawnMonster(
            state,
            new SpawnMonsterDebugCommand("rat", 1, 1)));
    }
}
