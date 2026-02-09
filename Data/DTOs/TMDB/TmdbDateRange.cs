using System.Text.Json.Serialization;

namespace FilmesAPI.DTO;

public class TmdbDateRange
{
    [JsonPropertyName("maximum")]
    public string Maximum { get; set; }

    [JsonPropertyName("minimum")]
    public string Minimum { get; set; }
}