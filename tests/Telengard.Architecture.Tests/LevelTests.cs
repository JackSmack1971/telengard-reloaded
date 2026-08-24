using Telengard.Core.Progression;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class LevelTests
{
    [Fact]
    public void Level_up_commits_one_configured_level_at_the_inn()
    {
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState { Level = 1, Experience = 10 }
        };

        var result = LevelResolver.LevelUp(
            state,
            new LevelUpCommand(),
            new LevelConfiguration([0, 10, 30]));

        Assert.Equal(2, result.State.Player.Level);
        Assert.Equal(10, result.State.Player.Experience);
        var leveledUp = Assert.IsType<PlayerLeveledUpEvent>(Assert.Single(result.Events));
        Assert.Equal(1, leveledUp.PreviousLevel);
        Assert.Equal(2, leveledUp.Level);
        Assert.Equal(10, leveledUp.Experience);
    }

    [Fact]
    public void Level_up_can_be_repeated_for_each_eligible_level()
    {
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState { Level = 1, Experience = 30 }
        };
        var configuration = new LevelConfiguration([0, 10, 30]);

        var second = LevelResolver.LevelUp(
            LevelResolver.LevelUp(state, new LevelUpCommand(), configuration).State,
            new LevelUpCommand(),
            configuration);

        Assert.Equal(3, second.State.Player.Level);
        Assert.Equal(2, Assert.IsType<PlayerLeveledUpEvent>(Assert.Single(second.Events)).PreviousLevel);
    }

    [Fact]
    public void Level_up_validates_state_and_threshold_before_mutation()
    {
        var command = new LevelUpCommand();
        var configuration = new LevelConfiguration([0, 10]);
        var away = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Player = new PlayerState { Experience = 10 }
        };
        var active = GameState.Create(1234) with
        {
            Expedition = new ExpeditionState { Active = true },
            Player = new PlayerState { Experience = 10 }
        };
        var dead = GameState.Create(1234) with
        {
            Player = new PlayerState { Alive = false, Experience = 10 }
        };
        var insufficient = GameState.Create(1234) with
        {
            Player = new PlayerState { Experience = 9 }
        };
        var negativeExperience = GameState.Create(1234) with
        {
            Player = new PlayerState { Experience = -1 }
        };
        var invalidLevel = GameState.Create(1234) with
        {
            Player = new PlayerState { Level = 0, Experience = 10 }
        };
        var maximum = GameState.Create(1234) with
        {
            Player = new PlayerState { Level = 2, Experience = 10 }
        };
        var integerMaximum = GameState.Create(1234) with
        {
            Player = new PlayerState { Level = int.MaxValue, Experience = 10 }
        };

        Assert.Throws<InvalidOperationException>(() => LevelResolver.LevelUp(away, command, configuration));
        Assert.Throws<InvalidOperationException>(() => LevelResolver.LevelUp(active, command, configuration));
        Assert.Throws<InvalidOperationException>(() => LevelResolver.LevelUp(dead, command, configuration));
        Assert.Throws<InvalidOperationException>(() => LevelResolver.LevelUp(insufficient, command, configuration));
        Assert.Throws<InvalidOperationException>(() => LevelResolver.LevelUp(negativeExperience, command, configuration));
        Assert.Throws<InvalidOperationException>(() => LevelResolver.LevelUp(invalidLevel, command, configuration));
        Assert.Throws<InvalidOperationException>(() => LevelResolver.LevelUp(maximum, command, configuration));
        Assert.Throws<InvalidOperationException>(() => LevelResolver.LevelUp(integerMaximum, command, configuration));
        Assert.Equal(1, away.Player.Level);
        Assert.Equal(1, insufficient.Player.Level);
    }

    [Fact]
    public void Level_configuration_validates_and_copies_thresholds()
    {
        Assert.Throws<ArgumentNullException>(() => new LevelConfiguration(null!));
        Assert.Throws<ArgumentException>(() => new LevelConfiguration([]));
        Assert.Throws<ArgumentException>(() => new LevelConfiguration([1, 2]));
        Assert.Throws<ArgumentException>(() => new LevelConfiguration([0, -1]));
        Assert.Throws<ArgumentException>(() => new LevelConfiguration([0, 10, 10]));

        var thresholds = new long[] { 0, 10, 30 };
        var configuration = new LevelConfiguration(thresholds);
        thresholds[1] = 99;

        Assert.Equal([0L, 10L, 30L], configuration.ExperienceThresholds);
        Assert.Equal(3, configuration.MaximumLevel);
        Assert.Equal(10, configuration.GetRequiredExperience(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => configuration.GetRequiredExperience(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => configuration.GetRequiredExperience(4));
    }

    [Fact]
    public void Equal_level_ups_replay_and_round_trip_deterministically()
    {
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState { Level = 1, Experience = 11 }
        };
        var command = new LevelUpCommand();
        var configuration = new LevelConfiguration([0, 11]);

        var first = LevelResolver.LevelUp(state, command, configuration);
        var second = LevelResolver.LevelUp(state, command, configuration);
        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(first.State));

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
        Assert.Equal(first.State.Player.Level, restored.Player.Level);
        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
    }
}
