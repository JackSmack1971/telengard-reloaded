using Telengard.Core.Knowledge;
using Telengard.Core.Simulation;

namespace Telengard.Core.Combat;

public enum ThreatLevel
{
    Trivial,
    Dangerous,
    Deadly,
    Unknown
}

public sealed record ThreatClassificationConfiguration
{
    public ThreatClassificationConfiguration(
        int trivialMaximumLevelDifference,
        int deadlyMinimumLevelDifference,
        IEnumerable<string>? knownMonsterDefinitionIds = null)
    {
        if (trivialMaximumLevelDifference >= deadlyMinimumLevelDifference)
        {
            throw new ArgumentException(
                "The trivial threshold must be lower than the deadly threshold.",
                nameof(trivialMaximumLevelDifference));
        }

        TrivialMaximumLevelDifference = trivialMaximumLevelDifference;
        DeadlyMinimumLevelDifference = deadlyMinimumLevelDifference;
        KnownMonsterDefinitionIds = Array.AsReadOnly((knownMonsterDefinitionIds ?? []).Select(id =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return id;
        }).Distinct(StringComparer.Ordinal).ToArray());
    }

    public int TrivialMaximumLevelDifference { get; }
    public int DeadlyMinimumLevelDifference { get; }
    public IReadOnlyList<string> KnownMonsterDefinitionIds { get; }
}

public sealed record AssessThreatCommand : ICommand;

public sealed record ThreatAssessedEvent(Guid EncounterId, ThreatLevel Level) : IDomainEvent;

public static class ThreatAssessmentResolver
{
    public static CommandResult Resolve(
        GameState state,
        AssessThreatCommand command,
        ThreatClassificationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(configuration);

        if (!state.Expedition.Active) throw new InvalidOperationException("Threat assessment requires an active expedition.");
        if (!state.Player.Alive) throw new InvalidOperationException("A dead player cannot assess a threat.");

        var combat = state.Combat ?? throw new InvalidOperationException("No combat is active.");
        if (combat.Phase != CombatPhase.ThreatAssessment)
        {
            throw new InvalidOperationException("Threat can be assessed only during the threat-assessment phase.");
        }

        var level = Classify(combat.Monster, state.Player, configuration, state.Knowledge);
        var nextCombat = combat with { Phase = CombatPhase.PlayerAction, ThreatLevel = level };
        return new CommandResult(
            state with { Combat = nextCombat },
            [new ThreatAssessedEvent(combat.EncounterId, level)]);
    }

    public static ThreatLevel Classify(
        MonsterInstance monster,
        PlayerState player,
        ThreatClassificationConfiguration configuration)
        => Classify(monster, player, configuration, null);

    public static ThreatLevel Classify(
        MonsterInstance monster,
        PlayerState player,
        ThreatClassificationConfiguration configuration,
        KnowledgeState? knowledge)
    {
        ArgumentNullException.ThrowIfNull(monster);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(configuration);

        var knownByConfiguration = configuration.KnownMonsterDefinitionIds.Contains(
            monster.DefinitionId,
            StringComparer.Ordinal);
        var knownByJournal = knowledge is not null && MonsterKnowledgeResolver.IsKnown(
            knowledge,
            monster.DefinitionId);
        if (!knownByConfiguration && !knownByJournal)
        {
            return ThreatLevel.Unknown;
        }

        var levelDifference = monster.Level - player.Level;
        if (levelDifference <= configuration.TrivialMaximumLevelDifference) return ThreatLevel.Trivial;
        if (levelDifference >= configuration.DeadlyMinimumLevelDifference) return ThreatLevel.Deadly;
        return ThreatLevel.Dangerous;
    }
}
