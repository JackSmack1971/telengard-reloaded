using System.Text.Json;
using Telengard.Core.Simulation;
using Telengard.Save.Dto;

namespace Telengard.Save;

public static class SaveGameSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string Serialize(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return JsonSerializer.Serialize(GameStateSaveDto.FromState(state), JsonOptions);
    }

    public static GameState Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        try
        {
            var save = JsonSerializer.Deserialize<GameStateSaveDto>(json, JsonOptions)
                ?? throw new SaveFormatException("Save document is empty.");
            return SaveMigrations.Migrate(save).ToState();
        }
        catch (JsonException exception)
        {
            throw new SaveFormatException("Save document is invalid JSON.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new SaveFormatException("Save document contains invalid state.", exception);
        }
    }
}
