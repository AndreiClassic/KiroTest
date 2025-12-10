# NZ House Insurance Calculator - Project Information

## Project Overview
A full-stack web application for calculating house insurance premiums in New Zealand. The application considers various factors including house value, age, location, construction type, number of bedrooms, and hazard zones (flood and earthquake risk) to provide comprehensive insurance quotes. Features include external lot data integration, geospatial hazard assessment, and property report generation.

## Tech Stack

### Backend
- **Framework**: .NET 9 Web API
- **Language**: C#
- **Port**: 5000
- **Key Features**:
  - RESTful API endpoint: `POST /api/insurance/calculate`
  - External Lot API integration: `GET /api/lot/{lotId}`
  - Hazard zone assessment (flood and earthquake)
  - Geocoding service (OpenStreetMap/Nominatim)
  - PostGIS integration for geospatial flood data
  - MongoDB integration for storing quotes and caching flood queries
  - Flood admin endpoints for data import
  - CORS enabled for frontend communication
  - HttpClient service for external API calls

### Frontend
- **Framework**: Vue 3 with TypeScript
- **Build Tool**: Vite
- **Router**: Vue Router
- **State Management**: Pinia
- **Dev Port**: 5173
- **Production Port**: 8080 (via nginx in Docker)
- **Key Features**:
  - Lot ID lookup with auto-population
  - Hazard zone display with color-coded badges
  - Real-time insurance calculation with hazard factors
  - Property report download (PDF/TXT format)
  - Responsive form validation
  - Coordinate display for geocoded locations

### Databases

#### MongoDB
- **Port**: 27017
- **Purpose**: Store insurance quotes and cache flood zone queries
- **Collections**: 
  - `quotes` - Insurance quotes with lot information
  - `floodQueries` - Cached flood zone lookups with geospatial indexing

#### PostGIS (PostgreSQL + GIS Extension)
- **Port**: 5432
- **Database**: floodmaps
- **Purpose**: Store and query flood polygon data for accurate geospatial analysis
- **Tables**:
  - `auckland_flood_zones` - Flood polygons with categories and return periods
- **Features**:
  - Geospatial indexing (GIST) for fast polygon queries
  - ST_Contains queries for point-in-polygon lookups
  - Support for GeoJSON import from LINZ/council data

### Logging & Monitoring
- **Logging Framework**: Serilog
- **Log Storage**: Elasticsearch
- **Log Visualization**: Kibana
- **Elasticsearch Port**: 9201 (external), 9200 (internal)
- **Kibana Port**: 5602 (external), 5601 (internal)
- **Log Index**: insurance-logs-{date}
- **Features**:
  - Automatic error/exception logging
  - HTTP request logging
  - Structured logging with context
  - Centralized log aggregation

### Infrastructure
- **Containerization**: Docker
- **Orchestration**: Docker Compose
- **Web Server**: Nginx (for production frontend)
- **Network**: Bridge network connecting all services
- **Services**:
  - Backend (.NET 9 API)
  - Frontend (Vue 3 + Nginx)
  - MongoDB (document storage)
  - PostGIS (geospatial database)
  - Elasticsearch (log storage)
  - Kibana (log visualization)

## Project Structure

```
.
├── backend/
│   ├── Controllers/
│   │   ├── InsuranceController.cs    # Insurance calculation API
│   │   ├── LotController.cs          # Lot data integration API
│   │   └── FloodAdminController.cs   # Flood data import/management
│   ├── Models/
│   │   ├── InsuranceRequest.cs       # Request DTO (with hazard zones)
│   │   ├── InsuranceResponse.cs      # Response DTO
│   │   ├── Lot/
│   │   │   ├── LotApiResponse.cs     # External API response model
│   │   │   └── LotData.cs            # Processed lot data with hazards
│   │   └── Hazard/
│   │       └── HazardInfo.cs         # Hazard zone information
│   ├── Services/
│   │   ├── ILotService.cs            # Lot service interface
│   │   ├── LotService.cs             # External API integration
│   │   ├── IHazardService.cs         # Hazard service interface
│   │   ├── HazardService.cs          # Geocoding & hazard assessment
│   │   ├── IFloodMapService.cs       # Flood map service interface
│   │   └── FloodMapService.cs        # PostGIS + MongoDB flood queries
│   ├── DataAccess/
│   │   ├── Configuration/
│   │   │   └── MongoDbSettings.cs    # MongoDB configuration
│   │   ├── Models/
│   │   │   ├── InsuranceQuote.cs     # MongoDB document model
│   │   │   └── FloodQuery.cs         # Cached flood query model
│   │   ├── Repositories/
│   │   │   ├── IInsuranceQuoteRepository.cs
│   │   │   ├── InsuranceQuoteRepository.cs
│   │   │   ├── IFloodQueryRepository.cs
│   │   │   └── FloodQueryRepository.cs
│   │   └── DataAccess.csproj         # DataAccess project
│   ├── Program.cs                     # App configuration with DI
│   ├── appsettings.json               # Configuration (MongoDB, PostGIS, LINZ API)
│   ├── Dockerfile                     # Backend container config
│   └── backend.csproj
│
├── frontend/
│   ├── src/
│   │   ├── views/
│   │   │   └── HomeView.vue          # Main calculator UI with Lot lookup
│   │   ├── App.vue                    # Root component
│   │   └── main.ts
│   ├── vite.config.ts                 # Vite config with proxy
│   ├── nginx.conf                     # Production web server config
│   ├── Dockerfile                     # Frontend container config
│   └── package.json
│
├── docker-compose.yml                 # Full stack: All services
├── docker-compose.dev.yml             # Dev: backend only in Docker
├── run-prod.bat                       # Quick start for production
├── run-dev.bat                        # Quick start for development
├── stop.bat                           # Stop all containers
├── import-flood-data.bat              # Import flood data to PostGIS
├── FLOOD-DATA-SETUP.md                # Flood data import guide
├── PROJECT-INFO.md                    # This file
└── .dockerignore

```

## Insurance Calculation Logic

The premium calculation considers:

1. **Base Rate**: 0.3% of house value
2. **Age Factor**:
   - Houses > 50 years: 1.3x multiplier
   - Houses > 30 years: 1.15x multiplier
   - Houses ≤ 30 years: 1.0x multiplier
3. **Construction Type Factor**:
   - Brick: 0.9x (lower risk)
   - Concrete: 0.85x (lowest risk)
   - Weatherboard: 1.1x (higher risk)
   - Other: 1.0x
4. **Location Factor** (NZ-specific):
   - Auckland: 1.1x
   - Wellington: 1.2x (earthquake risk)
   - Christchurch: 1.15x (earthquake risk)
   - Other: 1.0x
5. **Flood Zone Factor** (NEW):
   - High: 1.3x (frequent flooding areas)
   - Medium: 1.15x (occasional flooding)
   - Low: 1.0x (minimal flood risk)
6. **Earthquake Zone Factor** (NEW):
   - High: 1.25x (major fault lines)
   - Medium: 1.1x (moderate seismic activity)
   - Low: 1.0x (stable areas)

**Risk Level Classification**:
- High: Premium > 0.4% of house value
- Medium: Premium > 0.3% of house value
- Low: Premium ≤ 0.3% of house value

**Formula**:
```
Annual Premium = House Value × Base Rate × Age Factor × Construction Factor × Location Factor × Flood Factor × Earthquake Factor
```

## Running the Application

### Option 1: Full Production (Everything in Docker)
```bash
run-prod.bat
# or
docker-compose up --build
```
- Frontend: http://localhost:8080
- Backend: http://localhost:5000
- MongoDB: localhost:27017
- PostGIS: localhost:5432
- Kibana (Logs): http://localhost:5602
- Elasticsearch: http://localhost:9201

### Option 2: Development Mode (Backend in Docker, Frontend local)
```bash
# Terminal 1: Start backend
run-dev.bat
# or
docker-compose -f docker-compose.dev.yml up

# Terminal 2: Start frontend with hot reload
cd frontend
npm install
npm run dev
```
- Frontend: http://localhost:5173 (with hot reload)
- Backend: http://localhost:5000

### Stop Everything
```bash
stop.bat
# or
docker-compose down
docker-compose -f docker-compose.dev.yml down
```

## API Endpoints

### Calculate Insurance Premium
**POST** `/api/insurance/calculate`

Calculates insurance premium and saves quote to MongoDB.

**Request Body**:
```json
{
  "houseValue": 500000,
  "buildYear": 2000,
  "location": "Auckland",
  "constructionType": "Brick",
  "bedrooms": 3,
  "floodZone": "Low",
  "earthquakeZone": "Medium"
}
```

**Response**:
```json
{
  "annualPremium": 1815.00,
  "monthlyPremium": 151.25,
  "riskLevel": "Medium"
}
```

### Get Lot Data
**GET** `/api/lot/{lotId}`

Fetches lot information from external API, geocodes the address, determines hazard zones, and returns processed data for insurance calculation.

**Example**: `GET /api/lot/1156`

**Response**:
```json
{
  "address": "6 Topfield Place, Morningside",
  "location": "Whangarei City",
  "region": "Northland",
  "floorArea": 130.5,
  "landArea": 441.0,
  "bedrooms": 3,
  "bathrooms": 1,
  "buildType": "1 Level Standalone",
  "buildYear": 2025,
  "estimatedHouseValue": 456750,
  "mappedLocation": "Whangarei City",
  "mappedConstructionType": "Other",
  "floodZone": "Low",
  "earthquakeZone": "Medium",
  "latitude": -35.7275,
  "longitude": 174.3166
}
```

### Get Insurance Estimate from Lot
**GET** `/api/lot/{lotId}/insurance-estimate`

Fetches lot data and returns insurance estimate in one call.

**Example**: `GET /api/lot/1156/insurance-estimate`

**Response**:
```json
{
  "lotId": 1156,
  "address": "6 Topfield Place, Morningside",
  "location": "Whangarei City",
  "estimatedHouseValue": 456750,
  "floorArea": 130.5,
  "bedrooms": 3,
  "buildYear": 2025,
  "floodZone": "Low",
  "earthquakeZone": "Medium",
  "annualPremium": 1507.28,
  "monthlyPremium": 125.61,
  "riskLevel": "Low"
}
```

### Flood Admin Endpoints
**POST** `/api/floodadmin/initialize`

Initializes PostGIS database with required tables and indexes.

**POST** `/api/floodadmin/import-auckland`

Imports Auckland flood data from GeoJSON file (requires file at `/app/data/auckland_flood.geojson`).

**POST** `/api/floodadmin/generate-sample-data`

Generates sample flood polygons for testing (Auckland CBD area).

## Dependencies

### Backend NuGet Packages
- **MongoDB.Driver** (2.30.0) - MongoDB client for .NET
- **Serilog** - Structured logging framework
- **Serilog.Sinks.Elasticsearch** - Elasticsearch sink for Serilog
- **Npgsql** (8.0.5) - PostgreSQL/PostGIS client for .NET
- **Microsoft.AspNetCore.OpenApi** - OpenAPI/Swagger support
- **Swashbuckle.AspNetCore** - Swagger UI

### Frontend NPM Packages
- **vue** (^3.x) - Progressive JavaScript framework
- **vue-router** (^4.x) - Official router for Vue.js
- **pinia** (^2.x) - State management for Vue
- **typescript** (^5.x) - TypeScript language support
- **vite** (^5.x) - Next generation frontend tooling

## Development Guidelines

### Backend Development
- Use `dotnet run` in the `backend/` directory for local development
- API runs on port 5000
- CORS is configured for localhost:8080 and localhost:5173
- All API responses are logged to Elasticsearch
- Use dependency injection for services (registered in Program.cs)

### Frontend Development
- Use `npm run dev` in the `frontend/` directory
- Vite dev server runs on port 5173
- API proxy configured in `vite.config.ts` for `/api` routes
- Hot module replacement (HMR) enabled
- TypeScript strict mode enabled

### Docker Development
- Backend Dockerfile uses multi-stage build (SDK → Runtime)
- Frontend Dockerfile uses multi-stage build (Node → Nginx)
- All services connected via `app-network` bridge network
- Use `docker-compose restart backend` for quick backend restarts
- Use `docker-compose up -d --build backend` when code changes require rebuild

## External API Integration

### Lot Management API
The application integrates with an external Lot Management API:
- **Base URL**: `http://host.docker.internal:52022/externalapi/lots`
- **Endpoint**: `GET /externalapi/lots/{lotId}`
- **Purpose**: Fetch property details for insurance calculation
- **Connection**: Uses `host.docker.internal` to access host machine from Docker container
- **Data Extracted**:
  - Address and location information (street, suburb, city, region)
  - Floor area and land area (amenities)
  - Number of bedrooms/bathrooms
  - Build type and estimated completion year
  - Amenities and property specifications
- **Logging**: Full JSON response logged for debugging and audit purposes

### Geocoding Service
- **Provider**: OpenStreetMap Nominatim
- **Purpose**: Convert addresses to latitude/longitude coordinates
- **Endpoint**: `https://nominatim.openstreetmap.org/search`
- **Rate Limiting**: Free tier with usage limits
- **Alternative**: Can be replaced with LINZ Address API or Google Maps Geocoding

### Hazard Assessment Services

#### Flood Zone Determination (3-Tier Fallback)
1. **MongoDB Cache**: Check for previously queried coordinates (30-day cache)
2. **PostGIS Query**: Query flood polygons for exact geospatial match
3. **Heuristic Fallback**: Use known flood-prone areas if no data available

#### Earthquake Zone Determination
- **Method**: Heuristic based on NZ geography and known fault lines
- **High Risk**: Wellington, Christchurch, Marlborough, Kaikoura
- **Medium Risk**: Auckland (volcanic field), rest of North Island
- **Low Risk**: Other areas
- **Future**: Can integrate with GeoNet API for real-time seismic data

## Database Schema

### MongoDB Collections

#### InsuranceQuote Collection
```json
{
  "_id": "ObjectId",
  "houseValue": 500000,
  "buildYear": 2000,
  "location": "Auckland",
  "constructionType": "Brick",
  "bedrooms": 3,
  "annualPremium": 1650.00,
  "monthlyPremium": 137.50,
  "riskLevel": "Low",
  "createdAt": "2025-12-10T00:00:00Z"
}
```

#### FloodQuery Collection (Cache)
```json
{
  "_id": "ObjectId",
  "latitude": -36.8485,
  "longitude": 174.7633,
  "parcelId": "1156",
  "floodZone": "Low",
  "region": "Auckland",
  "floodCategory": "Low Risk",
  "returnPeriod": 100,
  "location": {
    "type": "Point",
    "coordinates": [174.7633, -36.8485]
  },
  "queriedAt": "2025-12-10T00:00:00Z",
  "expiresAt": "2026-01-09T00:00:00Z"
}
```
**Indexes**:
- `location` (2dsphere) - Geospatial index for nearby queries
- `expiresAt` (TTL) - Automatic cleanup of expired cache entries

### PostGIS Tables

#### auckland_flood_zones
```sql
CREATE TABLE auckland_flood_zones (
    id SERIAL PRIMARY KEY,
    flood_category VARCHAR(100),
    return_period DECIMAL(10,2),
    geometry GEOMETRY(MULTIPOLYGON, 4326),
    source VARCHAR(255),
    imported_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_auckland_flood_geom 
ON auckland_flood_zones USING GIST (geometry);
```

**Sample Data**:
- Flood polygons from LINZ or council GeoJSON files
- Categories: High Risk, Medium Risk, Low Risk
- Return periods: 20-year, 50-year, 100-year flood events

## Environment Variables

### Backend
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: http://+:5000
- `MongoDb__ConnectionString`: mongodb://admin:password123@mongodb:27017
- `MongoDb__DatabaseName`: insurance
- `PostGIS__ConnectionString`: Host=postgis;Port=5432;Database=floodmaps;Username=postgres;Password=postgres123
- `LinzApi__ApiKey`: 806b800401634a688db8e557bd8e78ec

### MongoDB
- `MONGO_INITDB_ROOT_USERNAME`: admin
- `MONGO_INITDB_ROOT_PASSWORD`: password123
- `MONGO_INITDB_DATABASE`: insurance

### PostGIS
- `POSTGRES_USER`: postgres
- `POSTGRES_PASSWORD`: postgres123
- `POSTGRES_DB`: floodmaps

### Frontend
- No environment variables required for basic setup
- API URL can be configured in Vite config

## Key Features

### Lot ID Integration with Hazard Assessment
1. User enters Lot ID from external system
2. Backend fetches property data from external API
3. System automatically:
   - Estimates house value based on floor area ($3,500/m²)
   - Geocodes address to get latitude/longitude coordinates
   - Determines flood zone (PostGIS → MongoDB cache → heuristic)
   - Determines earthquake zone (heuristic based on NZ geography)
   - Maps location to insurance risk zones
   - Maps build type to construction categories
   - Extracts build year from completion dates
4. Frontend displays hazard zones with color-coded badges
5. Form auto-populates with calculated values including hazard factors
6. User can adjust values before calculating premium
7. Premium calculation includes hazard zone multipliers

### Property Report Download
- Download comprehensive property report after insurance calculation
- Includes lot information, hazard zones, site coverage, and insurance quote
- Format: Text file (TXT) with structured layout
- Future: Can be upgraded to PDF with proper formatting

### Data Flow
```
External Lot API → LotService → HazardService → FloodMapService
                                      ↓              ↓
                                  Geocoding    PostGIS Query
                                      ↓              ↓
                              Hazard Assessment  MongoDB Cache
                                      ↓
                              LotController → Frontend
                                      ↓
                          Display hazard zones & auto-populate form
                                      ↓
                          InsuranceController (with hazard factors)
                                      ↓
                                  MongoDB
```

## Logging Strategy

### What Gets Logged
- **Automatic**: All HTTP requests (method, path, status, duration)
- **Automatic**: All unhandled exceptions and errors
- **Manual Info Logs**:
  - Lot data fetch requests (Lot ID)
  - Full JSON response from external Lot API (for debugging)
  - Successful lot data retrieval (Lot ID, Address)
  - Insurance calculation requests (House Value, Location)
  - Quote saved to database (Premium, Risk Level)
  - Application startup/shutdown

### Viewing Logs
1. Open Kibana: http://localhost:5602
2. Go to "Discover" in the left menu
3. Create index pattern: `insurance-logs-*`
4. View real-time logs with filtering and search

### Log Levels
- **Information**: Normal operations (API calls, data retrieval)
- **Warning**: Lot not found, external service issues
- **Error**: Exceptions, database errors (automatic)
- **Fatal**: Application startup failures (automatic)

## Version Control

The project uses Git with a comprehensive `.gitignore` file that excludes:
- .NET build artifacts (bin/, obj/, *.dll, *.exe, *.pdb)
- IDE files (Visual Studio, Rider, VS Code)
- NuGet packages
- Node.js dependencies (node_modules/)
- Environment files (.env)
- Logs and temporary files

## Hazard Zone Assessment Details

### Flood Zone Determination (3-Tier System)

#### Tier 1: MongoDB Cache (Fastest)
- Checks for previously queried coordinates within 100m radius
- 30-day cache for PostGIS results
- 7-day cache for heuristic results
- Geospatial 2dsphere index for fast lookups

#### Tier 2: PostGIS Query (Most Accurate)
- Queries actual flood polygon data from LINZ/council sources
- Uses ST_Contains for point-in-polygon matching
- Maps flood categories to High/Medium/Low zones
- Considers return periods (20-year, 50-year, 100-year events)
- Currently ready for Auckland flood data import

#### Tier 3: Heuristic Fallback (Always Available)
- Based on known flood-prone areas in NZ
- **High Risk Areas**:
  - Westport (frequent flooding)
  - Thames/Coromandel (coastal flooding)
  - Lower Hutt (Hutt River flooding)
  - Edgecumbe (2017 floods)
- **Medium Risk Areas**:
  - Nelson/Tasman
  - Whanganui (river flooding)
  - Gisborne (coastal and river)
- **Low Risk**: All other areas

### Earthquake Zone Determination

Uses heuristic based on NZ geography and known fault lines:

- **High Risk**:
  - Wellington region (major fault lines)
  - Christchurch region (recent major earthquakes)
  - Marlborough/Kaikoura (Alpine Fault)
- **Medium Risk**:
  - Auckland (volcanic field)
  - Rest of North Island (general seismic activity)
- **Low Risk**: Other areas

### Importing Real Flood Data

See `FLOOD-DATA-SETUP.md` for detailed instructions on:
1. Downloading flood data from LINZ Data Service
2. Converting Shapefiles to GeoJSON
3. Importing data into PostGIS
4. Testing the import

Quick import command:
```bash
import-flood-data.bat
```

## Future Enhancements
- ✅ Database for storing quotes (MongoDB implemented)
- ✅ External API integration for lot data
- ✅ Centralized logging with Elasticsearch and Kibana
- ✅ Git version control with proper .gitignore
- ✅ Flood zone assessment (PostGIS + MongoDB cache)
- ✅ Earthquake zone assessment (heuristic)
- ✅ Property report download (TXT format)
- ✅ Geocoding service integration
- Import real flood data for all NZ regions (currently Auckland-ready)
- Integrate GeoNet API for real-time earthquake risk
- Upgrade property report to proper PDF format
- Implement user authentication
- Email quote functionality
- Compare multiple insurance providers
- Add unit tests for calculation logic
- Add E2E tests for user flows
- Add API endpoint to retrieve saved quotes
- Implement quote history and comparison features
- Add admin dashboard for quote management
- Add application metrics and performance monitoring
- Add coastal erosion risk assessment
- Add liquefaction risk assessment
- Add tsunami risk assessment for coastal properties
