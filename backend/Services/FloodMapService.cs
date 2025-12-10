using DataAccess.Models;
using DataAccess.Repositories;
using Npgsql;
using System.Text.Json;

namespace backend.Services;

public class FloodMapService : IFloodMapService
{
    private readonly IFloodQueryRepository _floodQueryRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FloodMapService> _logger;
    private readonly string _connectionString;

    public FloodMapService(
        IFloodQueryRepository floodQueryRepository,
        IConfiguration configuration,
        ILogger<FloodMapService> logger)
    {
        _floodQueryRepository = floodQueryRepository;
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration["PostGIS:ConnectionString"]
                            ?? throw new InvalidOperationException("PostGIS connection string not configured");
    }

    public async Task<FloodZoneResult> GetFloodZoneAsync(decimal latitude, decimal longitude, string? parcelId = null)
    {
        try
        {
            // Step 1: Check MongoDB cache
            var cachedQuery = await _floodQueryRepository.GetCachedQueryAsync(latitude, longitude);
            if (cachedQuery != null)
            {
                _logger.LogInformation("Flood zone found in cache for Lat={Lat}, Long={Long}", latitude, longitude);
                return new FloodZoneResult
                {
                    FloodZone = cachedQuery.FloodZone,
                    Region = cachedQuery.Region,
                    FloodCategory = cachedQuery.FloodCategory,
                    ReturnPeriod = cachedQuery.ReturnPeriod,
                    FromCache = true,
                    Source = "PostGIS-Cached"
                };
            }

            // Step 2: Query PostGIS for flood polygons
            var floodData = await QueryPostGISAsync(latitude, longitude);

            if (floodData != null)
            {
                _logger.LogInformation("Flood zone found in PostGIS for Lat={Lat}, Long={Long}: {Zone}",
                    latitude, longitude, floodData.FloodZone);

                // Step 3: Cache the result in MongoDB
                var query = new FloodQuery
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    ParcelId = parcelId,
                    FloodZone = floodData.FloodZone,
                    Region = floodData.Region,
                    FloodCategory = floodData.FloodCategory,
                    ReturnPeriod = floodData.ReturnPeriod,
                    QueriedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30) // Cache for 30 days
                };

                await _floodQueryRepository.SaveQueryAsync(query);

                return floodData;
            }

            // Step 4: Fallback to heuristic
            _logger.LogInformation("No PostGIS data found, using heuristic for Lat={Lat}, Long={Long}",
                latitude, longitude);

            var heuristicZone = DetermineFloodZoneHeuristic(latitude, longitude);

            // Cache heuristic result too
            var heuristicQuery = new FloodQuery
            {
                Latitude = latitude,
                Longitude = longitude,
                ParcelId = parcelId,
                FloodZone = heuristicZone,
                Region = DetermineRegion(latitude, longitude),
                QueriedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7) // Shorter cache for heuristic
            };

            await _floodQueryRepository.SaveQueryAsync(heuristicQuery);

            return new FloodZoneResult
            {
                FloodZone = heuristicZone,
                Region = heuristicQuery.Region,
                FromCache = false,
                Source = "Heuristic"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flood zone for Lat={Lat}, Long={Long}", latitude, longitude);
            return new FloodZoneResult
            {
                FloodZone = "Low",
                Region = "Unknown",
                Source = "Error"
            };
        }
    }

    private async Task<FloodZoneResult?> QueryPostGISAsync(decimal latitude, decimal longitude)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Query flood polygons that contain this point
            // Assuming table structure: auckland_flood_zones (id, geometry, flood_category, return_period)
            var sql = @"
                SELECT 
                    flood_category,
                    return_period,
                    ST_AsText(geometry) as geom_text
                FROM auckland_flood_zones
                WHERE ST_Contains(
                    geometry,
                    ST_SetSRID(ST_MakePoint(@longitude, @latitude), 4326)
                )
                ORDER BY return_period DESC
                LIMIT 1";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("latitude", (double)latitude);
            cmd.Parameters.AddWithValue("longitude", (double)longitude);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var floodCategory = reader.GetString(0);
                var returnPeriod = reader.IsDBNull(1) ? (decimal?)null : reader.GetDecimal(1);

                // Map flood category to risk level
                var floodZone = MapFloodCategoryToZone(floodCategory, returnPeriod);

                return new FloodZoneResult
                {
                    FloodZone = floodZone,
                    Region = "Auckland",
                    FloodCategory = floodCategory,
                    ReturnPeriod = returnPeriod,
                    FromCache = false,
                    Source = "PostGIS"
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying PostGIS for Lat={Lat}, Long={Long}", latitude, longitude);
            return null;
        }
    }

    private string MapFloodCategoryToZone(string category, decimal? returnPeriod)
    {
        // Map flood categories to High/Medium/Low zones
        var categoryLower = category.ToLower();

        if (categoryLower.Contains("high") || (returnPeriod.HasValue && returnPeriod.Value <= 20))
            return "High";

        if (categoryLower.Contains("medium") || categoryLower.Contains("moderate") ||
            (returnPeriod.HasValue && returnPeriod.Value <= 100))
            return "Medium";

        return "Low";
    }

    private string DetermineFloodZoneHeuristic(decimal latitude, decimal longitude)
    {
        // Known flood-prone areas in Auckland
        // Auckland CBD/Waterfront: -36.84, 174.76
        if (IsNearLocation(latitude, longitude, -36.84m, 174.76m, 0.02m))
        {
            return "Medium";
        }

        // West Auckland (Waitakere): -36.85, 174.55
        if (IsNearLocation(latitude, longitude, -36.85m, 174.55m, 0.1m))
        {
            return "Medium";
        }

        // Default for Auckland region
        if (latitude >= -37.1m && latitude <= -36.6m && longitude >= 174.5m && longitude <= 175.0m)
        {
            return "Low";
        }

        return "Low";
    }

    private string DetermineRegion(decimal latitude, decimal longitude)
    {
        // Auckland region
        if (latitude >= -37.1m && latitude <= -36.6m && longitude >= 174.5m && longitude <= 175.0m)
            return "Auckland";

        return "Unknown";
    }

    private bool IsNearLocation(decimal lat1, decimal lon1, decimal lat2, decimal lon2, decimal threshold)
    {
        var latDiff = Math.Abs(lat1 - lat2);
        var lonDiff = Math.Abs(lon1 - lon2);
        return latDiff <= threshold && lonDiff <= threshold;
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Enable PostGIS extension
            await using var cmd1 = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS postgis;", conn);
            await cmd1.ExecuteNonQueryAsync();

            // Create Auckland flood zones table
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS auckland_flood_zones (
                    id SERIAL PRIMARY KEY,
                    flood_category VARCHAR(100),
                    return_period DECIMAL(10,2),
                    geometry GEOMETRY(MULTIPOLYGON, 4326),
                    source VARCHAR(255),
                    imported_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
                
                CREATE INDEX IF NOT EXISTS idx_auckland_flood_geom 
                ON auckland_flood_zones USING GIST (geometry);
            ";

            await using var cmd2 = new NpgsqlCommand(createTableSql, conn);
            await cmd2.ExecuteNonQueryAsync();

            _logger.LogInformation("PostGIS database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing PostGIS database");
            throw;
        }
    }

    public async Task<bool> ImportAucklandFloodDataAsync(string geoJsonFilePath)
    {
        try
        {
            if (!File.Exists(geoJsonFilePath))
            {
                _logger.LogWarning("GeoJSON file not found: {Path}", geoJsonFilePath);
                return false;
            }

            var geoJson = await File.ReadAllTextAsync(geoJsonFilePath);
            var doc = JsonDocument.Parse(geoJson);

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var features = doc.RootElement.GetProperty("features");
            var importCount = 0;

            foreach (var feature in features.EnumerateArray())
            {
                var properties = feature.GetProperty("properties");
                var geometry = feature.GetProperty("geometry");

                // Try multiple property name variations (different councils use different names)
                var floodCategory =
                    TryGetPropertyValue(properties, "flood_category") ??
                    TryGetPropertyValue(properties, "FloodCategory") ??
                    TryGetPropertyValue(properties, "FLOOD_CAT") ??
                    TryGetPropertyValue(properties, "Category") ??
                    TryGetPropertyValue(properties, "CATEGORY") ??
                    TryGetPropertyValue(properties, "Risk") ??
                    TryGetPropertyValue(properties, "RISK") ??
                    "Unknown";

                decimal? returnPeriod = null;
                var rpValue =
                    TryGetPropertyValue(properties, "return_period") ??
                    TryGetPropertyValue(properties, "ReturnPeriod") ??
                    TryGetPropertyValue(properties, "RETURN_PER") ??
                    TryGetPropertyValue(properties, "ARI") ??
                    TryGetPropertyValue(properties, "Years");

                if (rpValue != null && decimal.TryParse(rpValue, out var rp))
                {
                    returnPeriod = rp;
                }

                var geomJson = geometry.GetRawText();

                var sql = @"
                    INSERT INTO auckland_flood_zones (flood_category, return_period, geometry, source)
                    VALUES (@category, @return_period, ST_GeomFromGeoJSON(@geom), @source)";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("category", floodCategory ?? "Unknown");
                cmd.Parameters.AddWithValue("return_period", returnPeriod ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("geom", geomJson);
                cmd.Parameters.AddWithValue("source", geoJsonFilePath);

                await cmd.ExecuteNonQueryAsync();
                importCount++;
            }

            _logger.LogInformation("Imported {Count} flood polygons from {Path}", importCount, geoJsonFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing Auckland flood data from {Path}", geoJsonFilePath);
            return false;
        }

        string? TryGetPropertyValue(JsonElement properties, string propertyName)
        {
            if (properties.TryGetProperty(propertyName, out var value))
            {
                return value.ValueKind == System.Text.Json.JsonValueKind.String
                    ? value.GetString()
                    : value.ToString();
            }

            return null;
        }
    }
}