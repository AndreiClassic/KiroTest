using System.Text.Json;
using backend.Models.Lot;

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
            _logger.LogInformation("Received lot data from external API for Lot {LotId}: {JsonResponse}", 
                lotId, json);
            
            var lotResponse = JsonSerializer.Deserialize<LotApiResponse>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (lotResponse?.ResultObject == null)
            {
                _logger.LogWarning("Failed to deserialize lot data for Lot {LotId}", lotId);
                return null;
            }

            var result = lotResponse.ResultObject;
            var floorArea = result.Amenities?.FloorArea ?? 0;
            var location = result.Location?.Area?.Label ?? result.LotAddress?.City ?? "";
            var buildType = result.BuildType?.Label ?? "";

            return new LotData
            {
                Address = $"{result.LotAddress?.Street}, {result.LotAddress?.Suburb}",
                Location = location,
                Region = result.Location?.Region?.Label ?? result.LotAddress?.RegionName ?? "",
                FloorArea = floorArea,
                LandArea = result.Amenities?.LandArea ?? 0,
                Bedrooms = result.Amenities?.NoBedrooms ?? 0,
                Bathrooms = result.Amenities?.NoBathrooms ?? 0,
                BuildType = buildType,
                BuildYear = ExtractBuildYear(result.JobComplete?.Estimate),
                
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
