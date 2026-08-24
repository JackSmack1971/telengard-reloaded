using Telengard.Content;
using Telengard.Core.Combat;
using Telengard.Core.Magic;
using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class FirstSliceSpellRosterTests
{
    [Fact]
    public void Production_pack_contains_a_valid_distinct_first_slice_roster()
    {
        var pack = ContentPackLoader.Load(RepositoryContentRoot());

        Assert.InRange(pack.Spells.Count, 6, 8);
        Assert.Equal(pack.Spells.Count, pack.Spells.Definitions.Keys.Distinct(StringComparer.Ordinal).Count());
        Assert.All(pack.Spells.Definitions.Values, spell =>
        {
            Assert.NotEmpty(spell.Name);
            Assert.NotEmpty(spell.InitialDescription);
            Assert.NotEmpty(spell.DiscoveredDescriptions);
            Assert.NotEmpty(spell.TargetingRule);
            Assert.NotEmpty(spell.Effects);
            Assert.NotEmpty(spell.Interactions);
            Assert.True(spell.Cost >= 0);
        });
    }

    [Fact]
    public void Loading_is_canonical_and_a_loaded_definition_is_consumable_by_casting()
    {
        var first = ContentPackLoader.Load(RepositoryContentRoot());
        var second = ContentPackLoader.Load(RepositoryContentRoot());

        Assert.Equal(first.Spells.Definitions.Keys, second.Spells.Definitions.Keys);
        Assert.Equal(first.Spells.Definitions.Values.Select(Fingerprint), second.Spells.Definitions.Values.Select(Fingerprint));
        Assert.All(first.Spells.Definitions.Values, spell => Assert.IsAssignableFrom<ISpellDefinition>(spell));

        var state = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Expedition = new ExpeditionState { Active = true },
            Player = new PlayerState
            {
                Position = new DungeonPosition(1, 0, 0),
                HitPoints = 10,
                MaxHitPoints = 10,
                SpellPower = 5,
                MaxSpellPower = 5,
                Spells = ["ember-bolt"]
            },
            Combat = new CombatState(
                new MonsterInstance(
                    Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    "rat",
                    1,
                    3,
                    new DungeonPosition(1, 0, 0)),
                CombatPhase.Resolution,
                selectedAction: CombatAction.CastSpell)
        };

        var result = SpellCastResolver.Resolve(
            state,
            new CastSpellCommand("ember-bolt"),
            first.Spells.GetRequired("ember-bolt"));

        Assert.Empty(state.Knowledge.Entries);
        Assert.Empty(result.State.Knowledge.Entries);
        Assert.Equal(2, result.State.Player.SpellPower);
        Assert.Equal("ember-bolt", Assert.IsType<SpellCastEvent>(result.Events[0]).SpellId);
    }

    private static string Fingerprint(SpellDefinition spell) => string.Join(
        "|",
        spell.Id,
        spell.Name,
        spell.InitialDescription,
        string.Join(",", spell.DiscoveredDescriptions),
        spell.Cost,
        spell.TargetingRule,
        string.Join(",", spell.Effects),
        string.Join(",", spell.Interactions));

    private static string RepositoryContentRoot() =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content");
}
