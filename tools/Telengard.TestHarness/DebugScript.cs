using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Telengard.Core.Combat;
using Telengard.Core.Economy;
using Telengard.Core.Rng;
using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Telengard.Save;

namespace Telengard.TestHarness;

public sealed record DebugScriptResult(
    GameState FinalState,
    IReadOnlyList<IDomainEvent> Events,
    string FinalSave,
    string Transcript,
    bool HadErrors);

public sealed class DebugScriptSession
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<int, FloorLayout> _layouts = [];
    private readonly List<IDomainEvent> _events = [];
    private string? _lastSave;
    private double _danger;

    public DebugScriptSession(GameState initialState)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        Dispatcher = CreateDispatcher(initialState);
    }

    public CommandDispatcher Dispatcher { get; private set; }
    public bool HadErrors { get; private set; }
    public IReadOnlyList<IDomainEvent> Events => _events;

    public DebugScriptResult Run(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var output = new List<string>();
        foreach (var line in lines)
        {
            var result = ExecuteLine(line);
            if (result is not null) output.Add(result);
        }

        return new DebugScriptResult(
            Dispatcher.CurrentState,
            _events.ToArray(),
            SaveGameSerializer.Serialize(Dispatcher.CurrentState),
            string.Join(Environment.NewLine, output),
            HadErrors);
    }

    public string? ExecuteLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#')) return null;

        var commandText = line.Trim();
        try
        {
            var tokens = Tokenize(commandText);
            if (tokens.Count == 0) return null;
            var eventStart = _events.Count;
            var output = Execute(tokens);
            return Json(new
            {
                type = "command",
                command = commandText,
                ok = true,
                output,
                events = _events
                    .Skip(eventStart)
                    .Select(EventNode)
                    .ToArray(),
                state = StateNode()
            });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or FormatException or OverflowException)
        {
            HadErrors = true;
            return Json(new
            {
                type = "command",
                command = commandText,
                ok = false,
                error = new { kind = exception.GetType().Name, message = exception.Message }
            });
        }
    }

    private object Execute(IReadOnlyList<string> tokens)
    {
        switch (tokens[0].ToLowerInvariant())
        {
            case "enter":
                RequireExactCount(tokens, 1);
                Dispatch(new EnterDungeonCommand());
                return new { action = "enter" };
            case "move":
                RequireExactCount(tokens, 2);
                var direction = ParseEnum<MovementDirection>(tokens[1]);
                Dispatch(new MoveCommand(direction));
                return new { action = "move", direction = direction.ToString() };
            case "leave":
                RequireExactCount(tokens, 1);
                Dispatch(new LeaveDungeonCommand());
                return new { action = "leave" };
            case "teleport":
                RequireExactCount(tokens, 4);
                Dispatch(new TeleportDebugCommand(Position(tokens, 1)));
                return new { action = "teleport" };
            case "set":
                return ExecuteSet(tokens);
            case "give":
                return ExecuteGive(tokens);
            case "spawn":
                return ExecuteSpawn(tokens);
            case "reveal":
                return ExecuteReveal(tokens);
            case "trigger":
                RequireExactCount(tokens, 2);
                if (!tokens[1].Equals("death", StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException("Expected 'death' after 'trigger'.");
                }

                if (!Dispatcher.CurrentState.Expedition.Active || !Dispatcher.CurrentState.Player.Alive)
                {
                    throw new InvalidOperationException("Trigger death requires an active living player.");
                }

                Dispatch(new SetPlayerHitPointsDebugCommand(0));
                Dispatch(new PlayerDeathCommand());
                return new { action = "trigger death" };
            case "inspect":
                return ExecuteInspect(tokens);
            case "dump":
                return ExecuteDump(tokens);
            case "save":
                return ExecuteSave(tokens);
            case "load":
                return ExecuteLoad(tokens);
            case "treasure":
                RequireCount(tokens, 2);
                Dispatch(new AcquireTreasureCommand(ParseInt(tokens[1]), tokens.Skip(2)));
                return new { action = "treasure" };
            default:
                throw new FormatException($"Unknown debug command '{tokens[0]}'.");
        }
    }

    private object ExecuteSet(IReadOnlyList<string> tokens)
    {
        RequireExactCount(tokens, 3);
        switch (tokens[1].ToLowerInvariant())
        {
            case "hp":
                Dispatch(new SetPlayerHitPointsDebugCommand(ParseInt(tokens[2])));
                return new { action = "set hp", value = ParseInt(tokens[2]) };
            case "level":
                Dispatch(new SetPlayerLevelDebugCommand(ParseInt(tokens[2])));
                return new { action = "set level", value = ParseInt(tokens[2]) };
            case "danger":
                _danger = ParseDouble(tokens[2]);
                if (_danger is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(tokens), "Danger must be between zero and one.");
                return new { action = "set danger", value = _danger };
            default:
                throw new FormatException($"Unknown set target '{tokens[1]}'.");
        }
    }

    private object ExecuteGive(IReadOnlyList<string> tokens)
    {
        RequireExactCount(tokens, 3);
        switch (tokens[1].ToLowerInvariant())
        {
            case "item":
                Dispatch(new GrantItemDebugCommand(tokens[2]));
                return new { action = "give item", value = tokens[2] };
            case "gold":
                Dispatch(new GrantGoldDebugCommand(ParseInt(tokens[2])));
                return new { action = "give gold", value = ParseInt(tokens[2]) };
            default:
                throw new FormatException($"Unknown give target '{tokens[1]}'.");
        }
    }

    private object ExecuteSpawn(IReadOnlyList<string> tokens)
    {
        RequireCount(tokens, 2);
        switch (tokens[1].ToLowerInvariant())
        {
            case "monster":
                if (tokens.Count is < 3 or > 5) throw new FormatException("Use 'spawn monster <id> [level] [hp]'.");
                var level = tokens.Count > 3 ? ParseInt(tokens[3]) : 1;
                var hitPoints = tokens.Count > 4 ? ParseInt(tokens[4]) : 1;
                Dispatch(new SpawnMonsterDebugCommand(tokens[2], level, hitPoints));
                return new { action = "spawn monster", value = tokens[2] };
            case "feature":
                if (tokens.Count is not (3 or 6)) throw new FormatException("Use 'spawn feature <id> [floor x y]'.");
                var position = tokens.Count >= 6 ? Position(tokens, 3) : Dispatcher.CurrentState.Player.Position;
                Dispatch(new SpawnFeatureDebugCommand(tokens[2], position));
                return new { action = "spawn feature", value = tokens[2] };
            default:
                throw new FormatException($"Unknown spawn target '{tokens[1]}'.");
        }
    }

    private object ExecuteReveal(IReadOnlyList<string> tokens)
    {
        RequireCount(tokens, 2);
        switch (tokens[1].ToLowerInvariant())
        {
            case "tile":
                RequireExactCount(tokens, 5);
                Dispatch(new RevealMapDebugCommand(new[] { Position(tokens, 2) }));
                return new { action = "reveal tile" };
            case "floor":
                if (tokens.Count is < 2 or > 3) throw new FormatException("Use 'reveal floor [floor]'.");
                var floor = tokens.Count > 2 ? ParseInt(tokens[2]) : Dispatcher.CurrentState.Player.Position.Floor;
                var layout = Layout(floor);
                var positions = Enumerable.Range(0, layout.Width)
                    .SelectMany(x => Enumerable.Range(0, layout.Height)
                        .Select(y => new DungeonPosition(floor, x, y)))
                    .Where(layout.IsWalkable)
                    .ToArray();
                Dispatch(new RevealMapDebugCommand(positions));
                return new { action = "reveal floor", floor, tiles = positions.Length };
            default:
                throw new FormatException($"Unknown reveal target '{tokens[1]}'.");
        }
    }

    private object ExecuteInspect(IReadOnlyList<string> tokens)
    {
        RequireExactCount(tokens, 3);
        if (!tokens[1].Equals("rng", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("Expected 'rng' after 'inspect'.");
        }

        var stream = new DeterministicRng(
                Dispatcher.CurrentState.WorldSeed,
                Dispatcher.CurrentState.Versions.SimulationVersion)
            .CreateStream("debug-inspection", $"key:{tokens[2]}", $"tick:{Dispatcher.CurrentState.SimulationTick}");
        return new { action = "inspect rng", key = tokens[2], value = stream.NextUInt() };
    }

    private object ExecuteDump(IReadOnlyList<string> tokens)
    {
        RequireCount(tokens, 2);
        if (tokens[1].Equals("knowledge", StringComparison.OrdinalIgnoreCase) && tokens.Count == 2)
        {
            return new { action = "dump knowledge", value = JsonSerializer.SerializeToNode(Dispatcher.CurrentState.Knowledge, JsonOptions) };
        }

        RequireExactCount(tokens, 3);
        if (!tokens[1].Equals("game", StringComparison.OrdinalIgnoreCase) ||
            !tokens[2].Equals("state", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("Use 'dump game state' or 'dump knowledge'.");
        }

        return new { action = "dump game state", value = StateNode() };
    }

    private object ExecuteSave(IReadOnlyList<string> tokens)
    {
        if (tokens.Count > 2) throw new FormatException("Use 'save [path]'.");
        _lastSave = SaveGameSerializer.Serialize(Dispatcher.CurrentState);
        if (tokens.Count > 1)
        {
            File.WriteAllText(tokens[1], _lastSave);
        }

        return new { action = "save", path = tokens.Count > 1 ? tokens[1] : null, bytes = _lastSave.Length };
    }

    private object ExecuteLoad(IReadOnlyList<string> tokens)
    {
        if (tokens.Count > 2) throw new FormatException("Use 'load [path]'.");
        var save = tokens.Count > 1 ? File.ReadAllText(tokens[1]) : _lastSave;
        if (string.IsNullOrWhiteSpace(save)) throw new InvalidOperationException("No save is available to load.");
        Dispatcher = CreateDispatcher(SaveGameSerializer.Deserialize(save));
        _lastSave = save;
        return new { action = "load", path = tokens.Count > 1 ? tokens[1] : null };
    }

    private void Dispatch<TCommand>(TCommand command)
        where TCommand : ICommand
    {
        var result = Dispatcher.Dispatch(command);
        _events.AddRange(result.Events);
    }

    private CommandDispatcher CreateDispatcher(GameState state)
    {
        var dispatcher = new CommandDispatcher(state);
        dispatcher.Register<TeleportDebugCommand>(DeveloperDebugResolver.Teleport);
        dispatcher.Register<SetPlayerHitPointsDebugCommand>(DeveloperDebugResolver.SetHitPoints);
        dispatcher.Register<SetPlayerLevelDebugCommand>(DeveloperDebugResolver.SetLevel);
        dispatcher.Register<GrantItemDebugCommand>(DeveloperDebugResolver.GrantItem);
        dispatcher.Register<GrantGoldDebugCommand>(DeveloperDebugResolver.GrantGold);
        dispatcher.Register<SpawnMonsterDebugCommand>(DeveloperDebugResolver.SpawnMonster);
        dispatcher.Register<SpawnFeatureDebugCommand>(DeveloperDebugResolver.SpawnFeature);
        dispatcher.Register<RevealMapDebugCommand>(DeveloperDebugResolver.RevealMap);
        dispatcher.Register<EnterDungeonCommand>((state, command) =>
            DungeonWalkingResolver.Enter(state, command, Layout(1)));
        dispatcher.Register<MoveCommand>((state, command) =>
            DungeonWalkingResolver.Move(
                state,
                command,
                Layout(state.Player.Position.Floor),
                new EncounterTriggerConfiguration(
                    _danger,
                    _danger > 0 ? [new EncounterSpawnOption("debug-monster", 1, 1)] : [])));
        dispatcher.Register<LeaveDungeonCommand>((state, command) =>
            DungeonWalkingResolver.Leave(state, command, Layout(1)));
        dispatcher.Register<PlayerDeathCommand>(PlayerDeathResolver.Resolve);
        dispatcher.Register<AcquireTreasureCommand>(TreasureAcquisitionResolver.Resolve);
        return dispatcher;
    }

    private FloorLayout Layout(int floor)
    {
        if (!_layouts.TryGetValue(floor, out var layout))
        {
            layout = new FloorLayoutGenerator().Generate(
                Dispatcher.CurrentState.WorldSeed,
                Dispatcher.CurrentState.Versions.GeneratorVersion,
                floor);
            _layouts.Add(floor, layout);
        }

        return layout;
    }

    private JsonNode StateNode() => JsonNode.Parse(SaveGameSerializer.Serialize(Dispatcher.CurrentState))!;

    private static JsonNode EventNode(IDomainEvent domainEvent) => new JsonObject
    {
        ["type"] = domainEvent.GetType().Name,
        ["data"] = JsonSerializer.SerializeToNode(domainEvent, domainEvent.GetType(), JsonOptions)
    };

    private static string Json(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private static IReadOnlyList<string> Tokenize(string command)
    {
        var tokens = new List<string>();
        var token = new System.Text.StringBuilder();
        var quoted = false;
        foreach (var character in command)
        {
            if (character == '"')
            {
                quoted = !quoted;
            }
            else if (char.IsWhiteSpace(character) && !quoted)
            {
                if (token.Length > 0)
                {
                    tokens.Add(token.ToString());
                    token.Clear();
                }
            }
            else
            {
                token.Append(character);
            }
        }

        if (quoted) throw new FormatException("Unterminated quoted argument.");
        if (token.Length > 0) tokens.Add(token.ToString());
        return tokens;
    }

    private static DungeonPosition Position(IReadOnlyList<string> tokens, int start)
    {
        return new DungeonPosition(ParseInt(tokens[start]), ParseInt(tokens[start + 1]), ParseInt(tokens[start + 2]));
    }

    private static int ParseInt(string value) =>
        int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static double ParseDouble(string value) =>
        double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static T ParseEnum<T>(string value)
        where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var result) && Enum.IsDefined(result)
            ? result
            : throw new FormatException($"Unknown {typeof(T).Name} '{value}'.");

    private static void RequireCount(IReadOnlyList<string> tokens, int count)
    {
        if (tokens.Count < count) throw new FormatException($"Command '{tokens[0]}' has too few arguments.");
    }

    private static void RequireExactCount(IReadOnlyList<string> tokens, int count)
    {
        if (tokens.Count != count) throw new FormatException($"Command '{tokens[0]}' has an invalid argument count.");
    }

}
