using System.Text.Json.Serialization;
using RosterSync.Core.Internals;

namespace RosterSync.Core;

public class RosterEvent
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("user_id")] public int UserId { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; } = null!;

    [JsonPropertyName("flight_number")] public string? FlightNumber { get; set; }

    [JsonPropertyName("aircraft")] public string? Aircraft { get; set; }

    [JsonPropertyName("origin")] public string? Origin { get; set; }

    [JsonPropertyName("destination")] public string? Destination { get; set; }

    [JsonConverter(typeof(RosterDateTimeConverter))]
    [JsonPropertyName("start_time")] public DateTime StartTime { get; set; }

    [JsonConverter(typeof(RosterDateTimeConverter))]
    [JsonPropertyName("end_time")] public DateTime EndTime { get; set; }

    [JsonPropertyName("status")] public string Status { get; set; } = null!;

    [JsonConverter(typeof(RosterDateTimeConverter))]
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }
}