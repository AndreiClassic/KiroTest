using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FloodAdminController : ControllerBase
{
    private readonly IFloodMapService _floodMapService;
    private readonly ILogger<FloodAdminController> _logger;
    
    public FloodAdminController(IFloodMapService floodMapService, ILogger<FloodAdminController> logger)
    {
        _floodMapService = floodMapService;
        _logger = logger;
    }
    
    /// <summary>
    /// Initialize PostGIS database (create tables and indexes)
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeDatabase()
    {
        try
        {
            await _floodMapService.InitializeDatabaseAsync();
            return Ok(new { message = "PostGIS database initialized successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Import Auckland flood data from GeoJSON file
    /// POST with file path in body: { "filePath": "/path/to/auckland_flood.geojson" }
    /// </summary>
    [HttpPost("import-auckland")]
    public async Task<IActionResult> ImportAucklandData([FromBody] ImportRequest request)
    {
        try
        {
            var success = await _floodMapService.ImportAucklandFloodDataAsync(request.FilePath);
            
            if (success)
                return Ok(new { message = "Auckland flood data imported successfully" });
            else
                return BadRequest(new { error = "Failed to import data. Check file path and format." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import Auckland flood data");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Generate sample flood data for testing (Auckland CBD area)
    /// </summary>
    [HttpPost("generate-sample-data")]
    public async Task<IActionResult> GenerateSampleData()
    {
        try
        {
            await _floodMapService.InitializeDatabaseAsync();
            
            // Generate sample GeoJSON for Auckland CBD and surrounding areas
            var sampleGeoJson = GenerateSampleAucklandFloodData();
            var tempFile = Path.Combine(Path.GetTempPath(), "sample_auckland_flood.geojson");
            await System.IO.File.WriteAllTextAsync(tempFile, sampleGeoJson);
            
            var success = await _floodMapService.ImportAucklandFloodDataAsync(tempFile);
            
            System.IO.File.Delete(tempFile);
            
            if (success)
                return Ok(new { message = "Sample flood data generated and imported successfully" });
            else
                return BadRequest(new { error = "Failed to generate sample data" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate sample data");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    private string GenerateSampleAucklandFloodData()
    {
        // Sample flood zones for Auckland testing
        // These are simplified polygons around known areas
        return @"{
  ""type"": ""FeatureCollection"",
  ""features"": [
    {
      ""type"": ""Feature"",
      ""properties"": {
        ""flood_category"": ""High Risk"",
        ""return_period"": 20,
        ""area_name"": ""Auckland Waterfront""
      },
      ""geometry"": {
        ""type"": ""MultiPolygon"",
        ""coordinates"": [[[[174.76, -36.84], [174.77, -36.84], [174.77, -36.85], [174.76, -36.85], [174.76, -36.84]]]]
      }
    },
    {
      ""type"": ""Feature"",
      ""properties"": {
        ""flood_category"": ""Medium Risk"",
        ""return_period"": 50,
        ""area_name"": ""West Auckland""
      },
      ""geometry"": {
        ""type"": ""MultiPolygon"",
        ""coordinates"": [[[[174.54, -36.84], [174.56, -36.84], [174.56, -36.86], [174.54, -36.86], [174.54, -36.84]]]]
      }
    },
    {
      ""type"": ""Feature"",
      ""properties"": {
        ""flood_category"": ""Medium Risk"",
        ""return_period"": 100,
        ""area_name"": ""Manukau Harbour Area""
      },
      ""geometry"": {
        ""type"": ""MultiPolygon"",
        ""coordinates"": [[[[174.78, -36.95], [174.80, -36.95], [174.80, -36.97], [174.78, -36.97], [174.78, -36.95]]]]
      }
    },
    {
      ""type"": ""Feature"",
      ""properties"": {
        ""flood_category"": ""Low Risk"",
        ""return_period"": 100,
        ""area_name"": ""Central Auckland""
      },
      ""geometry"": {
        ""type"": ""MultiPolygon"",
        ""coordinates"": [[[[174.74, -36.86], [174.76, -36.86], [174.76, -36.88], [174.74, -36.88], [174.74, -36.86]]]]
      }
    }
  ]
}";
    }
    
    public class ImportRequest
    {
        public string FilePath { get; set; } = string.Empty;
    }
}
