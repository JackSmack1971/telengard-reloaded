using Telengard.Core.Progression;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class ExperienceTests
{
    [Fact]
    public void Award_commits_caller_supplied_experience_at_the_inn()
    {
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState { Experience = 12 }
        };

        var result = ExperienceResolver.Award(state, new AwardExperienceCommand(30));

        Assert.Equal(42, result.State.Player.Experience);
        var awarded = Assert.IsType<ExperienceAwardedEvent>(Assert.Single(result.Events));
        Assert.Equal(30, awarded.Amount);
        Assert.Equal(42, awarded.TotalExperience);
    }

    [Fact]
    public void Award_validates_commit_boundary_before_mutating()
    {
        var active = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Expedition = new ExpeditionState { Active = true }
        };
        var dead = GameState.Create(1234) with
        {
            Player = new PlayerState { Alive = false }
        };

        Assert.Throws<InvalidOperationException>(() => ExperienceResolver.Award(
            active,
            new AwardExperienceCommand(1)));
        Assert.Throws<InvalidOperationException>(() => ExperienceResolver.Award(
            dead,
            new AwardExperienceCommand(1)));
        Assert.Equal(0, active.Player.Experience);
        Assert.Equal(0, dead.Player.Experience);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Award_rejects_non_positive_amounts(long amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AwardExperienceCommand(amount));
    }

    [Fact]
    public void Award_rejects_negative_existing_experience_and_overflow()
    {
        var negative = GameState.Create(1234) with
        {
            Player = new PlayerState { Experience = -1 }
        };
        var overflowing = GameState.Create(1234) with
        {
            Player = new PlayerState { Experience = long.MaxValue }
        };

        Assert.Throws<InvalidOperationException>(() => ExperienceResolver.Award(
            negative,
            new AwardExperienceCommand(1)));
        Assert.Throws<OverflowException>(() => ExperienceResolver.Award(
            overflowing,
            new AwardExperienceCommand(1)));
    }

    [Fact]
    public void Equal_awards_replay_and_round_trip_deterministically()
    {
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState { Experience = 7 }
        };
        var command = new AwardExperienceCommand(11);

        var first = ExperienceResolver.Award(state, command);
        var second = ExperienceResolver.Award(state, command);
        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(first.State));

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
        Assert.Equal(first.State.Player.Experience, restored.Player.Experience);
        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
    }
}
