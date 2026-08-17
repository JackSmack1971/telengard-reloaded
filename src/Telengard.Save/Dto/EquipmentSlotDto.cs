using System.Text.Json;
using System.Text.Json.Serialization;
using Telengard.Core.Items;

namespace Telengard.Save.Dto;

[JsonConverter(typeof(EquipmentSlotDtoConverter))]
public sealed record EquipmentSlotDto
{
    public required string SlotId { get; init; }
    public Guid? ItemInstanceId { get; init; }

    public static EquipmentSlotDto FromState(EquipmentSlotState slot) => new()
    {
        SlotId = slot.SlotId,
        ItemInstanceId = slot.ItemInstanceId
    };

    public EquipmentSlotState ToState() => new(SlotId, ItemInstanceId);
}

public sealed class EquipmentSlotDtoConverter : JsonConverter<EquipmentSlotDto>
{
    public override EquipmentSlotDto Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new EquipmentSlotDto { SlotId = reader.GetString()! };
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("An equipment slot must be an object or legacy slot string.");
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var slotId = ReadString(root, "slotId", "SlotId");
        Guid? itemInstanceId = null;
        if (TryGetProperty(root, out var item, "itemInstanceId", "ItemInstanceId") &&
            item.ValueKind != JsonValueKind.Null)
        {
            if (item.ValueKind != JsonValueKind.String || !Guid.TryParse(item.GetString(), out var parsed))
            {
                throw new JsonException("An equipment item id must be a GUID or null.");
            }

            itemInstanceId = parsed;
        }

        return new EquipmentSlotDto { SlotId = slotId, ItemInstanceId = itemInstanceId };
    }

    public override void Write(
        Utf8JsonWriter writer,
        EquipmentSlotDto value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("slotId", value.SlotId);
        if (value.ItemInstanceId.HasValue)
        {
            writer.WriteString("itemInstanceId", value.ItemInstanceId.Value);
        }
        else
        {
            writer.WriteNull("itemInstanceId");
        }

        writer.WriteEndObject();
    }

    private static string ReadString(JsonElement root, params string[] names)
    {
        if (!TryGetProperty(root, out var value, names) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new JsonException("An equipment slot requires a nonblank slot id.");
        }

        return value.GetString()!;
    }

    private static bool TryGetProperty(JsonElement root, out JsonElement value, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }
}
