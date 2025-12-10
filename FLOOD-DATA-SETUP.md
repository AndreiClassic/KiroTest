# Flood Data Setup Guide

## Overview
The flood mapping system uses PostGIS to store Auckland flood zone polygons and MongoDB to cache queries for performance.

## Quick Start (Testing with Sample Data)

### 1. Start all services including PostGIS
```bash
docker-compose up -d --build
```

### 2. Initialize PostGIS database
```bash
curl -X POST http://localhost:5000/api/floodadmin/initialize
```

### 3. Generate and import sample flood data
```bash
curl -X POST http://localhost:5000/api/floodadmin/generate-sample-data
```

This creates sample flood zones for:
- Auckland Waterfront (High Risk)
- West Auckland (Medium Risk)
- Manukau Harbour Area (Medium Risk)
- Central Auckland (Low Risk)

### 4. Test the flood zone query
```bash
# Test Auckland CBD coordinates
curl http://localhost:5000/api/lot/1156
```

## Production Setup (Real Auckland Flood Data)

### Option 1: Auckland Council Open Data

1. **Visit Auckland Council Open Data Portal**
   - https://data-aucklandcouncil.opendata.arcgis.com/
   
2. **Search for flood datasets**
   - Search terms: "flood", "inundation", "overland flow"
   - Look for: "Flood Prone Areas", "Overland Flow Paths", "Flood Hazard"

3. **Download as GeoJSON**
   - Click on dataset
   - Export → GeoJSON
   - Save to your local machine

4. **Import into PostGIS**
   ```bash
   # Copy file to backend container
   docker cp auckland_flood.geojson insurance-backend:/app/data/auckland_flood.geojson
   
   # Import via API
   curl -X POST http://localhost:5000/api/floodadmin/import-auckland \
     -H "Content-Type: application/json" \
     -d '{"filePath": "/app/data/auckland_flood.geojson"}'
   ```

### Option 2: LINZ Data Service

1. **Visit LINZ Data Service**
   - https://data.linz.govt.nz/
   - Use API key: `806b800401634a688db8e557bd8e78ec`

2. **Search for relevant layers**
   - Search: "flood", "hazard", "risk"
   - Note: LINZ primarily has cadastral data, not comprehensive flood data

3. **Download and convert to GeoJSON**
   - Download as Shapefile
   - Convert using GDAL: `ogr2ogr -f GeoJSON output.geojson input.shp`

### Option 3: Manual GeoJSON Creation

Create a GeoJSON file with this structure:

```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "properties": {
        "flood_category": "High Risk",
        "return_period": 20,
        "area_name": "Your Area Name"
      },
      "geometry": {
        "type": "MultiPolygon",
        "coordinates": [[[[lng1, lat1], [lng2, lat2], [lng3, lat3], [lng1, lat1]]]]
      }
    }
  ]
}
```

## Data Flow

```
1. User requests lot data → LotService
2. LotService gets coordinates → HazardService
3. HazardService → FloodMapService.GetFloodZoneAsync()
4. FloodMapService checks:
   a. MongoDB cache (fast) → return if found
   b. PostGIS query (accurate) → cache result → return
   c. Heuristic fallback (if no data) → cache result → return
```

## Database Schema

### PostGIS Table: `auckland_flood_zones`
```sql
CREATE TABLE auckland_flood_zones (
    id SERIAL PRIMARY KEY,
    flood_category VARCHAR(100),      -- e.g., "High Risk", "Medium Risk"
    return_period DECIMAL(10,2),      -- e.g., 20, 50, 100 (years)
    geometry GEOMETRY(MULTIPOLYGON, 4326),
    source VARCHAR(255),
    imported_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### MongoDB Collection: `flood_queries`
```javascript
{
  _id: ObjectId,
  latitude: Decimal,
  longitude: Decimal,
  parcelId: String,
  floodZone: String,              // "High", "Medium", "Low"
  region: String,                 // "Auckland"
  floodCategory: String,          // Original category from PostGIS
  returnPeriod: Decimal,          // 20, 50, 100 year flood
  queriedAt: DateTime,
  expiresAt: DateTime             // Cache expiration
}
```

## API Endpoints

### Admin Endpoints (FloodAdminController)

**Initialize Database**
```bash
POST /api/floodadmin/initialize
```

**Import Auckland Data**
```bash
POST /api/floodadmin/import-auckland
Content-Type: application/json

{
  "filePath": "/path/to/auckland_flood.geojson"
}
```

**Generate Sample Data**
```bash
POST /api/floodadmin/generate-sample-data
```

### Query Endpoints (via LotController)

**Get Lot with Flood Zone**
```bash
GET /api/lot/{lotId}
```

Response includes:
```json
{
  "floodZone": "Medium",
  "earthquakeZone": "Medium",
  "latitude": -36.8485,
  "longitude": 174.7633
}
```

## Monitoring

### Check PostGIS Data
```bash
# Connect to PostGIS
docker exec -it insurance-postgis psql -U postgres -d floodmaps

# Query flood zones
SELECT flood_category, return_period, COUNT(*) 
FROM auckland_flood_zones 
GROUP BY flood_category, return_period;

# Check specific location
SELECT flood_category, return_period
FROM auckland_flood_zones
WHERE ST_Contains(
    geometry,
    ST_SetSRID(ST_MakePoint(174.7633, -36.8485), 4326)
);
```

### Check MongoDB Cache
```bash
# Connect to MongoDB
docker exec -it insurance-mongodb mongosh -u admin -p password123

use insurance
db.flood_queries.find().pretty()
db.flood_queries.countDocuments()
```

### Check Logs
```bash
# View backend logs
docker logs insurance-backend -f

# Check Kibana
http://localhost:5602
```

## Performance

- **First query**: ~200-500ms (PostGIS query + MongoDB write)
- **Cached query**: ~10-50ms (MongoDB read only)
- **Cache duration**: 30 days for PostGIS data, 7 days for heuristic
- **Cache radius**: ~100m (0.001 degrees)

## Troubleshooting

### PostGIS connection fails
```bash
# Check PostGIS is running
docker ps | grep postgis

# Check connection string in appsettings.json
# Should be: Host=postgis;Port=5432;Database=floodmaps;Username=postgres;Password=postgres123
```

### No flood data returned
```bash
# Check if data exists
docker exec -it insurance-postgis psql -U postgres -d floodmaps -c "SELECT COUNT(*) FROM auckland_flood_zones;"

# If 0, import sample data or real data
```

### Import fails
- Check GeoJSON format (must be valid GeoJSON with MultiPolygon geometry)
- Check file path is accessible from container
- Check logs for detailed error messages

## Future Enhancements

- Add more regions (Wellington, Christchurch)
- Integrate with regional council APIs for real-time data
- Add elevation data for better flood risk assessment
- Implement parcel-level caching by property ID
- Add admin UI for data management
