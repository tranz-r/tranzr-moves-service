using System.Text.Json.Serialization;

namespace AdminClientHandlerService.Infrastructure.Models
{
    public class VboxStatus
    {
        [JsonPropertyName("is_online")]
        public bool IsOnline { get; set; }
    }
}