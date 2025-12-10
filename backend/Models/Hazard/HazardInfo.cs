namespace backend.Models.Hazard;

public class HazardInfo
{
    public string FloodZone { get; set; } = "Low";
    public string EarthquakeZone { get; set; } = "Low";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class GeocodingResult
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string FormattedAddress { get; set; } = string.Empty;
}
