using Telengard.Content;
using Telengard.Core.Combat;
using Telengard.Core.Magic;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class SpellCastingTests
{
    [Fact]
    public void Casting_a_known_spell_spends_power_and_advances_combat()
    {
        var state = ActiveCombat(spellPower: 5, spells: ["ember-bolt"]);
        var definition = Definition("ember-bolt", cost: 3);

        var result = SpellCastResolver.Resolve(state, new CastSpellCommand("ember-bolt"), definition);

        Assert.Equal(2, result.State.Player.SpellPower);
        Assert.Equal(CombatPhase.EnemyAction, result.State.Combat!.Phase);
        Assert.Equal(CombatAction.CastSpell, result.State.Combat.SelectedAction);
        var cast = Assert.IsType<SpellCastEvent>(result.Events[0]);
        Assert.Equal(state.Combat!.EncounterId, cast.EncounterId);
        Assert.Equal("ember-bolt", cast.SpellId);
        Assert.Equal(3, cast.Cost);
        Assert.Equal(2, cast.RemainingSpellPower);
        var phaseChanged = Assert.IsType<CombatPhaseChangedEvent>(result.Events[1]);
        Assert.Equal(CombatPhase.Resolution, phaseChanged.From);
        Assert.Equal(CombatPhase.EnemyAction, phaseChanged.To);
    }

    [Fact]
    public void Casting_all_available_power_and_zero_cost_spells_are_valid()
    {
        var exact = SpellCastResolver.Resolve(
            ActiveCombat(spellPower: 3, spells: ["ember-bolt"]),
            new CastSpellCommand("ember-bolt"),
            Definition("ember-bolt", cost: 3));
        Assert.Equal(0, exact.State.Player.SpellPower);

        var free = SpellCastResolver.Resolve(
            ActiveCombat(spellPower: 0, spells: ["ward"]),
            new CastSpellCommand("ward"),
            Definition("ward", cost: 0));
        Assert.Equal(0, free.State.Player.SpellPower);
    }

    [Fact]
    public void Casting_validates_before_spending_power_or_advancing_combat()
    {
        var state = ActiveCombat(spellPower: 2, spells: ["ember-bolt"]);

        Assert.Throws<InvalidOperationException>(() => SpellCastResolver.Resolve(
            state,
            new CastSpellCommand("unknown"),
            Definition("unknown", cost: 1)));
        Assert.Throws<InvalidOperationException>(() => SpellCastResolver.Resolve(
            state,
            new CastSpellCommand("ember-bolt"),
            Definition("other", cost: 1)));
        Assert.Throws<ArgumentException>(() => SpellCastResolver.Resolve(
            state,
            new CastSpellCommand("ember-bolt"),
            new TestSpellDefinition("", 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => SpellCastResolver.Resolve(
            state,
            new CastSpellCommand("ember-bolt"),
            new TestSpellDefinition("ember-bolt", -1)));
        Assert.Throws<InvalidOperationException>(() => SpellCastResolver.Resolve(
            state,
            new CastSpellCommand("ember-bolt"),
            Definition("ember-bolt", cost: 3)));
        Assert.Throws<InvalidOperationException>(() => SpellCastResolver.Resolve(
            state with { Player = state.Player with { Spells = [] } },
            new CastSpellCommand("ember-bolt"),
            Definition("ember-bolt", cost: 1)));
        Assert.Equal(2, state.Player.SpellPower);
        Assert.Equal(CombatPhase.Resolution, state.Combat!.Phase);
    }

    [Fact]
    public void Casting_requires_live_active_selected_combat_and_valid_commands()
    {
        var state = ActiveCombat(spellPower: 2, spells: ["ember-bolt"]);
        var command = new CastSpellCommand("ember-bolt");
        var definition = Definition("ember-bolt", cost: 1);

        Assert.Throws<ArgumentException>(() => new CastSpellCommand(" "));
        Assert.Throws<ArgumentNullException>(() => SpellCastResolver.Resolve(null!, command, definition));
        Assert.Throws<ArgumentNullException>(() => SpellCastResolver.Resolve(state, null!, definition));
        Assert.Throws<ArgumentNullException>(() => SpellCastResolver.Resolve(state, command, null!));
        Assert.Throws<InvalidOperationException>(() => SpellCastResolver.Resolve(
            state with { Expedition = new ExpeditionState() }, command, definition));
        Assert.Throws<InvalidOperationException>(() => SpellCastResolver.Resolve(
            state with { Player = state.Player with { Alive = false } }, command, definition));
        Assert.Throws<InvalidOperationException>(() => SpellCastResolver.Resolve(
            state with { Combat = null }, command, definition));
        Assert.Throws<InvalidOperationException>(() => SpellCastResolver.Resolve(
            state with { Combat = state.Combat! with { Phase = CombatPhase.PlayerAction } }, command, definition));
        Assert.Throws<InvalidOperationException>(() => SpellCastResolver.Resolve(
            state with { Combat = state.Combat! with { SelectedAction = CombatAction.Attack } }, command, definition));
    }

    [Fact]
    public void Equal_cast_inputs_replay_and_round_trip_deterministically()
    {
        var state = ActiveCombat(spellPower: 7, spells: ["ember-bolt"]);
        var command = new CastSpellCommand("ember-bolt");
        var definition = Definition("ember-bolt", cost: 4);

        var first = SpellCastResolver.Resolve(state, command, definition);
        var second = SpellCastResolver.Resolve(state, command, definition);
        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(first.State));

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(restored));
        Assert.Equal(first.State.Player.SpellPower, restored.Player.SpellPower);
        Assert.Equal(first.State.Combat!.EncounterId, restored.Combat!.EncounterId);
        Assert.Equal(first.State.Combat.Phase, restored.Combat.Phase);
        Assert.Equal(first.State.Combat.SelectedAction, restored.Combat.SelectedAction);
        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
    }

    private static SpellDefinition Definition(string id, int cost) => new(
        id,
        id,
        "The spell's purpose is unclear.",
        [],
        cost,
        "single_target",
        ["unresolved_effect"]);

    private static GameState ActiveCombat(int spellPower, IReadOnlyList<string> spells)
    {
        var position = new DungeonPosition(1, 0, 0);
        var state = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Expedition = new ExpeditionState { Active = true },
            Player = new PlayerState
            {
                Position = position,
                HitPoints = 10,
                MaxHitPoints = 10,
                SpellPower = spellPower,
                MaxSpellPower = 10,
                Spells = spells
            }
        };

        return state with
        {
            Combat = new CombatState(
                new MonsterInstance(
                    Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    "rat",
                    1,
                    3,
                    position),
                CombatPhase.Resolution,
                selectedAction: CombatAction.CastSpell)
        };
    }

    private sealed record TestSpellDefinition(string Id, int Cost) : ISpellDefinition;
}
