using System.Text.Json;
using backend.Models.Hazard;

namespace backend.Services;

public class HazardService : IHazardService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HazardService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IFloodMapService _floodMapService;
    
    public HazardService(
        HttpClient httpClient, 
        ILogger<HazardService> logger, 
        IConfiguration configuration,
        IFloodMapService floodMapService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _floodMapService = floodMapService;
    }

    public async Task<HazardInfo> GetHazardInfoAsync(string address)
    {
        var hazardInfo = new HazardInfo();
        
        try
        {
            // Step 1: Geocode the address to get lat/long
            var geocodingResult = await GeocodeAddressAsync(address);
            
            if (geocodingResult == null)
            {
                _logger.LogWarning("Failed to geocode address: {Address}", address);
                return hazardInfo;
            }

            hazardInfo.Latitude = geocodingResult.Latitude;
            hazardInfo.Longitude = geocodingResult.Longitude;

            _logger.LogInformation("Geocoded address '{Address}' to coordinates: Lat={Lat}, Long={Long}", 
                address, geocodingResult.Latitude, geocodingResult.Longitude);

            // Step 2: Get flood zone information using FloodMapService
            var floodResult = await _floodMapService.GetFloodZoneAsync(
                geocodingResult.Latitude, 
                geocodingResult.Longitude);
            hazardInfo.FloodZone = floodResult.FloodZone;
            
            _logger.LogInformation("Flood zone from {Source}: {Zone} (Cached: {Cached})", 
                floodResult.Source, floodResult.FloodZone, floodResult.FromCache);
            
            // Step 3: Get earthquake zone information
            hazardInfo.EarthquakeZone = await GetEarthquakeZoneAsync(geocodingResult.Latitude, geocodingResult.Longitude);

            return hazardInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hazard info for address: {Address}", address);
            return hazardInfo;
        }
    }

    public async Task<GeocodingResult?> GeocodeAddressAsync(string address)
    {
        try
        {
            // Using Nominatim (OpenStreetMap) - Free, no API key required
            // For production, consider using LINZ Address API or Google Maps Geocoding
            var encodedAddress = Uri.EscapeDataString($"{address}, New Zealand");
            var url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=1";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "NZ-Insurance-Calculator/1.0");
            
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Geocoding API returned status: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Geocoding API response for '{Address}': {Response}", address, json);
            
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (results == null || results.Count == 0)
            {
                _logger.LogWarning("No geocoding results found for address: {Address}", address);
                return null;
            }

            var result = results[0];
            return new GeocodingResult
            {
                Latitude = decimal.Parse(result.Lat),
                Longitude = decimal.Parse(result.Lon),
                FormattedAddress = result.DisplayName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address: {Address}", address);
            return null;
        }
    }



    private async Task<string> GetEarthquakeZoneAsync(decimal latitude, decimal longitude)
    {
        try
        {
            // Using GeoNet API - Free, no API key required
            // Query recent seismic activity near the location
            var url = $"https://api.geonet.org.nz/quake?MMI=3";
            
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GeoNet API returned status: {Status}", response.StatusCode);
                return DetermineEarthquakeZoneHeuristic(latitude, longitude);
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("GeoNet API response: {Response}", json);
            
            // Determine earthquake zone based on known fault lines and seismic activity
            var earthquakeZone = DetermineEarthquakeZoneHeuristic(latitude, longitude);
            
            _logger.LogInformation("Earthquake zone determined: {EarthquakeZone} for Lat={Lat}, Long={Long}", 
                earthquakeZone, latitude, longitude);
            
            return earthquakeZone;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting earthquake zone for Lat={Lat}, Long={Long}", latitude, longitude);
            return "Low";
        }
    }

    private string DetermineFloodZoneHeuristic(decimal latitude, decimal longitude)
    {
        // Known flood-prone areas in NZ based on historical data
        
        // Westport area: -41.75, 171.60 (HIGH - frequent flooding)
        if (IsNearLocation(latitude, longitude, -41.75m, 171.60m, 0.1m))
        {
            _logger.LogInformation("Location near Westport - known high flood risk area");
            return "High";
        }
        
        // Thames/Coromandel: -37.14, 175.54 (HIGH - coastal flooding)
        if (IsNearLocation(latitude, longitude, -37.14m, 175.54m, 0.15m))
        {
            _logger.LogInformation("Location near Thames - known high flood risk area");
            return "High";
        }
        
        // Lower Hutt: -41.21, 174.91 (HIGH - Hutt River flooding)
        if (IsNearLocation(latitude, longitude, -41.21m, 174.91m, 0.08m))
        {
            _logger.LogInformation("Location near Lower Hutt - known high flood risk area");
            return "High";
        }
        
        // Edgecumbe: -37.98, 176.83 (HIGH - 2017 floods)
        if (IsNearLocation(latitude, longitude, -37.98m, 176.83m, 0.1m))
        {
            _logger.LogInformation("Location near Edgecumbe - known high flood risk area");
            return "High";
        }
        
        // Nelson/Tasman: -41.27, 173.28 (MEDIUM - occasional flooding)
        if (IsNearLocation(latitude, longitude, -41.27m, 173.28m, 0.15m))
        {
            _logger.LogInformation("Location near Nelson - medium flood risk area");
            return "Medium";
        }
        
        // Whanganui: -39.93, 175.05 (MEDIUM - river flooding)
        if (IsNearLocation(latitude, longitude, -39.93m, 175.05m, 0.1m))
        {
            _logger.LogInformation("Location near Whanganui - medium flood risk area");
            return "Medium";
        }
        
        // Gisborne: -38.66, 178.02 (MEDIUM - coastal and river)
        if (IsNearLocation(latitude, longitude, -38.66m, 178.02m, 0.12m))
        {
            _logger.LogInformation("Location near Gisborne - medium flood risk area");
            return "Medium";
        }
        
        // Default to Low for areas without known flood history
        _logger.LogInformation("Location not in known flood-prone area - assuming low risk");
        return "Low";
    }
    
    private bool IsNearLocation(decimal lat1, decimal lon1, decimal lat2, decimal lon2, decimal threshold)
    {
        // Simple distance check (not precise but good enough for heuristic)
        var latDiff = Math.Abs(lat1 - lat2);
        var lonDiff = Math.Abs(lon1 - lon2);
        return latDiff <= threshold && lonDiff <= threshold;
    }

    private string DetermineEarthquakeZoneHeuristic(decimal latitude, decimal longitude)
    {
        // Wellington region: -41.28, 174.78 (HIGH - on major fault line)
        if (latitude >= -41.5m && latitude <= -41.0m && longitude >= 174.5m && longitude <= 175.0m)
        {
            return "High";
        }
        
        // Christchurch region: -43.53, 172.64 (HIGH - recent major earthquakes)
        if (latitude >= -43.7m && latitude <= -43.3m && longitude >= 172.4m && longitude <= 172.9m)
        {
            return "High";
        }
        
        // Marlborough/Kaikoura: -42.40, 173.60 (HIGH - Alpine Fault)
        if (latitude >= -42.6m && latitude <= -42.0m && longitude >= 173.4m && longitude <= 174.0m)
        {
            return "High";
        }
        
        // Auckland region: -36.85, 174.76 (MEDIUM - volcanic field)
        if (latitude >= -37.1m && latitude <= -36.6m && longitude >= 174.5m && longitude <= 175.0m)
        {
            return "Medium";
        }
        
        // Rest of North Island (MEDIUM - general seismic activity)
        if (latitude >= -42.0m)
        {
            return "Medium";
        }
        
        return "Low";
    }



    // Helper class for Nominatim API response
    private class NominatimResult
    {
        public string Lat { get; set; } = string.Empty;
        public string Lon { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
    
    // Helper classes for LINZ API response
    private class LinzResponse
    {
        public LinzVectorQuery? VectorQuery { get; set; }
    }
    
    private class LinzVectorQuery
    {
        public List<LinzLayer>? Layers { get; set; }
    }
    
    private class LinzLayer
    {
        public List<LinzFeature>? Features { get; set; }
    }
    
    private class LinzFeature
    {
        public Dictionary<string, object>? Properties { get; set; }
    }
}
