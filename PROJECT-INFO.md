# NZ House Insurance Calculator - Project Information

## Project Overview
A full-stack web application for calculating house insurance premiums in New Zealand. The application considers various factors including house value, age, location, construction type, and number of bedrooms to provide insurance quotes.

## Tech Stack

### Backend
- **Framework**: .NET 9 Web API
- **Language**: C#
- **Port**: 5000
- **Key Features**:
  - RESTful API endpoint: `POST /api/insurance/calculate`
  - External Lot API integration: `GET /api/lot/{lotId}`
  - MongoDB integration for storing insurance quotes
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
  - Real-time insurance calculation
  - Responsive form validation

### Database
- **Database**: MongoDB
- **Port**: 27017
- **Purpose**: Store insurance quotes with lot information
- **Collections**: quotes

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

## Project Structure

```
.
├── backend/
│   ├── Controllers/
│   │   ├── InsuranceController.cs    # Insurance calculation API
│   │   └── LotController.cs          # Lot data integration API
│   ├── Models/
│   │   ├── InsuranceRequest.cs       # Request DTO
│   │   └── InsuranceResponse.cs      # Response DTO
│   ├── Services/
│   │   ├── ILotService.cs            # Lot service interface
│   │   └── LotService.cs             # External API integration
│   ├── DataAccess/
│   │   ├── Configuration/
│   │   │   └── MongoDbSettings.cs    # MongoDB configuration
│   │   ├── Models/
│   │   │   └── InsuranceQuote.cs     # MongoDB document model
│   │   ├── Repositories/
│   │   │   ├── IInsuranceQuoteRepository.cs
│   │   │   └── InsuranceQuoteRepository.cs
│   │   └── DataAccess.csproj         # DataAccess project
│   ├── Program.cs                     # App configuration with DI
│   ├── appsettings.json               # Configuration with MongoDB
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
├── docker-compose.yml                 # Full stack: MongoDB, Backend, Frontend
├── docker-compose.dev.yml             # Dev: backend only in Docker
├── run-prod.bat                       # Quick start for production
├── run-dev.bat                        # Quick start for development
├── stop.bat                           # Stop all containers
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

**Risk Level Classification**:
- High: Premium > 0.4% of house value
- Medium: Premium > 0.3% of house value
- Low: Premium ≤ 0.3% of house value

## Running the Application

### Option 1: Full Production (Everything in Docker)
```bash
run-prod.bat
# or
docker-compose up --build
```
- Frontend: http://localhost:8080
- Backend: http://localhost:5000
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
  "bedrooms": 3
}
```

**Response**:
```json
{
  "annualPremium": 1650.00,
  "monthlyPremium": 137.50,
  "riskLevel": "Low"
}
```

### Get Lot Data
**GET** `/api/lot/{lotId}`

Fetches lot information from external API and returns processed data for insurance calculation.

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
  "mappedLocation": "Other",
  "mappedConstructionType": "Other"
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
  "annualPremium": 1370.25,
  "monthlyPremium": 114.19,
  "riskLevel": "Low"
}
```

## Development Guidelines

### Backend Development
- Use `dotnet run` in the `backend/` directory for local development
- API runs on port 5000
- CORS is configured for localhost:8080 and localhost:5173

### Frontend Development
- Use `npm run dev` in the `frontend/` directory
- Vite dev server runs on port 5173
- API proxy configured in `vite.config.ts` for `/api` routes
- Hot module replacement (HMR) enabled

### Docker Development
- Backend Dockerfile uses multi-stage build (SDK → Runtime)
- Frontend Dockerfile uses multi-stage build (Node → Nginx)
- Both services connected via `app-network` bridge network

## External API Integration

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

## Database Schema

### InsuranceQuote Collection
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

## Environment Variables

### Backend
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: http://+:5000
- `MongoDb__ConnectionString`: MongoDB connection string
- `MongoDb__DatabaseName`: insurance

### MongoDB
- `MONGO_INITDB_ROOT_USERNAME`: admin
- `MONGO_INITDB_ROOT_PASSWORD`: password123
- `MONGO_INITDB_DATABASE`: insurance

### Frontend
- No environment variables required for basic setup
- API URL can be configured in Vite config

## Key Features

### Lot ID Integration
1. User enters Lot ID from external system
2. Backend fetches property data from external API
3. System automatically:
   - Estimates house value based on floor area ($3,500/m²)
   - Maps location to insurance risk zones
   - Maps build type to construction categories
   - Extracts build year from completion dates
4. Form auto-populates with calculated values
5. User can adjust values before calculating premium

### Data Flow
```
External Lot API → LotService → LotController → Frontend
                                      ↓
                              Auto-populate form
                                      ↓
                          InsuranceController → MongoDB
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

## Future Enhancements
- ✅ Database for storing quotes (MongoDB implemented)
- ✅ External API integration for lot data
- ✅ Centralized logging with Elasticsearch and Kibana
- ✅ Git version control with proper .gitignore
- Implement user authentication
- Add more NZ-specific risk factors (flood zones, earthquake zones)
- Email quote functionality
- Compare multiple insurance providers
- Add unit tests for calculation logic
- Add E2E tests for user flows
- Add API endpoint to retrieve saved quotes
- Implement quote history and comparison features
- Add admin dashboard for quote management
- Add application metrics and performance monitoring
- **PDF Download**: Add functionality to download insurance quote as PDF document
