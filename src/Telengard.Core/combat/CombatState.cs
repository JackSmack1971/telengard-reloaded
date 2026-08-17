using Telengard.Core.Simulation;

namespace Telengard.Core.Combat;

public enum CombatPhase
{
    Searching,
    Contact,
    ThreatAssessment,
    PlayerAction,
    Resolution,
    EnemyAction,
    StateCheck
}

public enum CombatAction
{
    Attack,
    Defend,
    Maneuver,
    CastSpell,
    UseItem,
    Flee,
    ContextualAction
}

public sealed record CombatState
{
    public CombatState(
        MonsterInstance monster,
        CombatPhase phase = CombatPhase.Contact,
        int round = 1,
        CombatAction? selectedAction = null,
        ThreatLevel? threatLevel = null)
    {
        ArgumentNullException.ThrowIfNull(monster);
        if (!Enum.IsDefined(phase)) throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown combat phase.");
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);
        if (selectedAction is not null && !Enum.IsDefined(selectedAction.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(selectedAction), selectedAction, "Unknown combat action.");
        }
        if (selectedAction is not null && phase is CombatPhase.Searching or CombatPhase.Contact or CombatPhase.ThreatAssessment or CombatPhase.PlayerAction)
        {
            throw new ArgumentException("A selected action is valid only after the player-action phase.", nameof(selectedAction));
        }
        if (threatLevel is not null && !Enum.IsDefined(threatLevel.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(threatLevel), threatLevel, "Unknown threat level.");
        }
        if (threatLevel is not null && phase is CombatPhase.Searching or CombatPhase.Contact or CombatPhase.ThreatAssessment)
        {
            throw new ArgumentException("A threat level is valid only after threat assessment.", nameof(threatLevel));
        }

        Monster = monster;
        Phase = phase;
        Round = round;
        SelectedAction = selectedAction;
        ThreatLevel = threatLevel;
    }

    public MonsterInstance Monster { get; init; }
    public Guid EncounterId => Monster.InstanceId;
    public CombatPhase Phase { get; init; }
    public int Round { get; init; }
    public CombatAction? SelectedAction { get; init; }
    public ThreatLevel? ThreatLevel { get; init; }
}

public sealed record AdvanceCombatCommand : ICommand;
public sealed record SelectCombatActionCommand(CombatAction Action) : ICommand;

public sealed record CombatPhaseChangedEvent(
    Guid EncounterId,
    CombatPhase From,
    CombatPhase To,
    int Round,
    CombatAction? Action = null) : IDomainEvent;

public static class CombatStateResolver
{
    public static CombatState Begin(MonsterInstance monster)
    {
        ArgumentNullException.ThrowIfNull(monster);
        return new CombatState(monster);
    }

    public static CommandResult Advance(GameState state, AdvanceCombatCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        var combat = RequireActive(state);

        if (combat.Phase == CombatPhase.Resolution && combat.SelectedAction is not null)
        {
            throw new InvalidOperationException("A selected combat action must be resolved before combat can advance.");
        }
        if (combat.Phase == CombatPhase.ThreatAssessment)
        {
            throw new InvalidOperationException("Threat assessment must be resolved before combat can advance.");
        }

        if (combat.Phase == CombatPhase.StateCheck && state.Player.HitPoints <= 0)
        {
            return PlayerDeathResolver.Resolve(state, new PlayerDeathCommand());
        }

        var (nextPhase, nextRound) = combat.Phase switch
        {
            CombatPhase.Searching => (CombatPhase.Contact, combat.Round),
            CombatPhase.Contact => (CombatPhase.ThreatAssessment, combat.Round),
            CombatPhase.Resolution => (CombatPhase.EnemyAction, combat.Round),
            CombatPhase.EnemyAction => (CombatPhase.StateCheck, combat.Round),
            CombatPhase.StateCheck => (CombatPhase.PlayerAction, checked(combat.Round + 1)),
            _ => throw new InvalidOperationException("Combat is waiting for a player action.")
        };

        return ChangePhase(state, combat, nextPhase, nextRound);
    }

    public static CommandResult SelectAction(GameState state, SelectCombatActionCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(command.Action))
        {
            throw new ArgumentOutOfRangeException(nameof(command), command.Action, "Unknown combat action.");
        }

        var combat = RequireActive(state);
        if (combat.Phase != CombatPhase.PlayerAction)
        {
            throw new InvalidOperationException("A combat action can be selected only during the player-action phase.");
        }

        var next = combat with { Phase = CombatPhase.Resolution, SelectedAction = command.Action };
        return new CommandResult(
            state with { Combat = next },
            [new CombatPhaseChangedEvent(combat.EncounterId, combat.Phase, next.Phase, next.Round, command.Action)]);
    }

    private static CommandResult ChangePhase(GameState state, CombatState combat, CombatPhase phase, int round)
    {
        var selectedAction = phase == CombatPhase.PlayerAction ? null : combat.SelectedAction;
        var threatLevel = phase is CombatPhase.Searching or CombatPhase.Contact or CombatPhase.ThreatAssessment
            ? null
            : combat.ThreatLevel;
        var next = combat with
        {
            Phase = phase,
            Round = round,
            SelectedAction = selectedAction,
            ThreatLevel = threatLevel
        };
        return new CommandResult(
            state with { Combat = next },
            [new CombatPhaseChangedEvent(combat.EncounterId, combat.Phase, phase, round, selectedAction)]);
    }

    private static CombatState RequireActive(GameState state)
    {
        if (!state.Expedition.Active) throw new InvalidOperationException("Combat requires an active expedition.");
        if (!state.Player.Alive) throw new InvalidOperationException("A dead player cannot act in combat.");
        return state.Combat ?? throw new InvalidOperationException("No combat is active.");
    }
}
