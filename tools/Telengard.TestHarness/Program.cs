using System.Text.Json;
using Telengard.Core.Simulation;
using Telengard.Save;

namespace Telengard.TestHarness;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            Console.Error.WriteLine(error);
            return 2;
        }

        GameState initialState;
        try
        {
            initialState = options.LoadPath is null
                ? GameState.Create(options.Seed!.Value)
                : SaveGameSerializer.Deserialize(File.ReadAllText(options.LoadPath));
        }
        catch (Exception exception) when (exception is IOException or SaveFormatException or ArgumentException)
        {
            Console.Error.WriteLine($"Load failed: {exception.Message}");
            return 1;
        }

        string[] lines;
        try
        {
            lines = options.ScriptPath is null
                ? Array.Empty<string>()
                : File.ReadAllLines(options.ScriptPath);
        }
        catch (IOException exception)
        {
            Console.Error.WriteLine($"Script load failed: {exception.Message}");
            return 1;
        }
        if (options.Deterministic)
        {
            var first = new DebugScriptSession(initialState).Run(lines);
            var second = new DebugScriptSession(initialState).Run(lines);
            if (first.HadErrors || second.HadErrors || first.FinalSave != second.FinalSave ||
                !EventSignatures(first.Events).SequenceEqual(EventSignatures(second.Events)))
            {
                Console.Error.WriteLine("Deterministic replay failed.");
                return 1;
            }

            WriteTranscript(first.Transcript);
            Console.WriteLine(JsonSerializer.Serialize(new { type = "determinism", ok = true }));
            return first.HadErrors ? 1 : 0;
        }

        var result = new DebugScriptSession(initialState).Run(lines);
        WriteTranscript(result.Transcript);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            type = "complete",
            ok = !result.HadErrors,
            seed = result.FinalState.WorldSeed,
            finalSave = result.FinalSave
        }));
        return result.HadErrors ? 1 : 0;
    }

    private static bool TryParseOptions(string[] args, out Options options, out string error)
    {
        long? seed = null;
        var deterministic = false;
        string? scriptPath = null;
        string? loadPath = null;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--seed" when index + 1 < args.Length && long.TryParse(args[++index], out var parsedSeed):
                    seed = parsedSeed;
                    break;
                case "--deterministic":
                    deterministic = true;
                    break;
                case "--script" when index + 1 < args.Length:
                    scriptPath = args[++index];
                    break;
                case "--load" when index + 1 < args.Length:
                    loadPath = args[++index];
                    break;
                default:
                    options = new Options(null, false, null, null);
                    error = "Usage: Telengard.TestHarness --seed <seed> [--deterministic] [--script <path>] [--load <path>]";
                    return false;
            }
        }

        if (seed is null && loadPath is null)
        {
            options = new Options(null, false, null, null);
            error = "Usage: Telengard.TestHarness --seed <seed> [--deterministic] [--script <path>] [--load <path>]";
            return false;
        }

        options = new Options(seed, deterministic, scriptPath, loadPath);
        error = string.Empty;
        return true;
    }

    private static void WriteTranscript(string transcript)
    {
        if (!string.IsNullOrEmpty(transcript)) Console.WriteLine(transcript);
    }

    private static IEnumerable<string> EventSignatures(IEnumerable<IDomainEvent> events) =>
        events.Select(domainEvent =>
            $"{domainEvent.GetType().AssemblyQualifiedName}:{JsonSerializer.Serialize(domainEvent, domainEvent.GetType())}");

    private sealed record Options(long? Seed, bool Deterministic, string? ScriptPath, string? LoadPath);
}
