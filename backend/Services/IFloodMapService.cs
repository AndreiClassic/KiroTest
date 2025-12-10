namespace backend.Services;

public interface IFloodMapService
{
    Task<FloodZoneResult> GetFloodZoneAsync(decimal latitude, decimal longitude, string? parcelId = null);
    Task InitializeDatabaseAsync();
    Task<bool> ImportAucklandFloodDataAsync(string geoJsonFilePath);
}

public class FloodZoneResult
{
    public string FloodZone { get; set; } = "Unknown";
    public string Region { get; set; } = string.Empty;
    public string? FloodCategory { get; set; }
    public decimal? ReturnPeriod { get; set; }
    public bool FromCache { get; set; }
    public string Source { get; set; } = "Heuristic";
}
