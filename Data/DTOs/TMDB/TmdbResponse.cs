using System.Text.Json.Serialization;

namespace FilmesAPI.DTO;

public class TmdbResponse
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("results")]
    public List<TmdbMovieResult> Results { get; set; } = new();

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("total_results")]
    public int TotalResults { get; set; }

    [JsonPropertyName("dates")]
    public TmdbDateRange? Dates { get; set; } // Nem todo endpoint retorna dates, então deixamos nullable
}