using System.Text.Json;

namespace backend.Services;

public class LotService : ILotService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LotService> _logger;
    private const string BaseUrl = "http://host.docker.internal:52022/externalapi/lots";

    public LotService(HttpClient httpClient, ILogger<LotService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LotData?> GetLotDataAsync(int lotId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/{lotId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch lot {LotId}. Status: {Status}", lotId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var lotResponse = JsonSerializer.Deserialize<LotApiResponse>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (lotResponse == null) return null;

            var floorArea = lotResponse.Amenities?.FloorArea ?? 0;
            var location = lotResponse.Location?.Area?.Label ?? lotResponse.LotAddress?.City ?? "";
            var buildType = lotResponse.BuildType?.Label ?? "";

            return new LotData
            {
                Address = $"{lotResponse.LotAddress?.Street}, {lotResponse.LotAddress?.Suburb}",
                Location = location,
                Region = lotResponse.Location?.Region?.Label ?? lotResponse.LotAddress?.RegionName ?? "",
                FloorArea = floorArea,
                LandArea = lotResponse.Amenities?.LandArea ?? 0,
                Bedrooms = lotResponse.Amenities?.NoBedrooms ?? 0,
                Bathrooms = lotResponse.Amenities?.NoBathrooms ?? 0,
                BuildType = buildType,
                BuildYear = ExtractBuildYear(lotResponse.JobComplete?.Estimate),
                
                // Pre-calculated insurance form values
                EstimatedHouseValue = (int)Math.Round(floorArea * 3500),
                MappedLocation = MapLocation(location),
                MappedConstructionType = MapConstructionType(buildType)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching lot data for {LotId}", lotId);
            return null;
        }
    }

    private string MapLocation(string location)
    {
        var locationLower = location.ToLower();
        
        if (locationLower.Contains("auckland")) return "Auckland";
        if (locationLower.Contains("wellington")) return "Wellington";
        if (locationLower.Contains("christchurch")) return "Christchurch";
        
        return "Other";
    }

    private string MapConstructionType(string buildType)
    {
        var buildTypeLower = buildType.ToLower();
        
        if (buildTypeLower.Contains("brick")) return "Brick";
        if (buildTypeLower.Contains("concrete")) return "Concrete";
        if (buildTypeLower.Contains("weatherboard")) return "Weatherboard";
        
        return "Other";
    }

    private int? ExtractBuildYear(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;
        
        if (DateTime.TryParse(dateString, out var date))
        {
            return date.Year;
        }
        
        return null;
    }
}

// API Response Models
public class LotApiResponse
{
    public LocationInfo? Location { get; set; }
    public LotAddressInfo? LotAddress { get; set; }
    public AmenitiesInfo? Amenities { get; set; }
    public BuildTypeInfo? BuildType { get; set; }
    public JobCompleteInfo? JobComplete { get; set; }
}

public class LocationInfo
{
    public RegionInfo? Region { get; set; }
    public DistrictInfo? District { get; set; }
    public AreaInfo? Area { get; set; }
}

public class RegionInfo
{
    public string Label { get; set; } = string.Empty;
}

public class DistrictInfo
{
    public string Label { get; set; } = string.Empty;
}

public class AreaInfo
{
    public string Label { get; set; } = string.Empty;
}

public class LotAddressInfo
{
    public string Street { get; set; } = string.Empty;
    public string Suburb { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
}

public class AmenitiesInfo
{
    public decimal FloorArea { get; set; }
    public decimal LandArea { get; set; }
    public int NoBedrooms { get; set; }
    public int NoBathrooms { get; set; }
}

public class BuildTypeInfo
{
    public string Label { get; set; } = string.Empty;
}

public class JobCompleteInfo
{
    public string? Estimate { get; set; }
}
