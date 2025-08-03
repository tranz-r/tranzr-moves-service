
using System.Text.Json.Serialization;

namespace AdminClientHandlerService.Infrastructure.Models
{
    public class TableResponse<T>
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("table")]
        public T[]? Table { get; set; }
    }
}