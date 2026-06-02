using System.Text.Json;

namespace TranzrMoves.Notifications.Contracts;

public static class NotificationTemplateData
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IReadOnlyDictionary<string, object?> FromObject(object data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions)
                   ?? new Dictionary<string, JsonElement>();
        return dict.ToDictionary(static kv => kv.Key, static kv => (object?)ElementToObject(kv.Value));
    }

    private static object? ElementToObject(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetDecimal(out var d) ? d : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ElementToObject).ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(static p => p.Name, static p => ElementToObject(p.Value)),
            _ => element.GetRawText()
        };
}
