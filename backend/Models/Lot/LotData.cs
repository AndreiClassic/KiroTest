namespace backend.Models.Lot;

public class LotData
{
    public string Address { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public decimal FloorArea { get; set; }
    public decimal LandArea { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public string BuildType { get; set; } = string.Empty;
    public int? BuildYear { get; set; }
    
    // Pre-calculated values for insurance form
    public int EstimatedHouseValue { get; set; }
    public string MappedLocation { get; set; } = string.Empty;
    public string MappedConstructionType { get; set; } = string.Empty;
    
    // Hazard zone information
    public string FloodZone { get; set; } = "Unknown";
    public string EarthquakeZone { get; set; } = "Unknown";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
