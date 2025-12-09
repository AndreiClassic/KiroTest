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
  - CORS enabled for frontend communication
  - Simple insurance calculation algorithm

### Frontend
- **Framework**: Vue 3 with TypeScript
- **Build Tool**: Vite
- **Router**: Vue Router
- **State Management**: Pinia
- **Dev Port**: 5173
- **Production Port**: 8080 (via nginx in Docker)

### Infrastructure
- **Containerization**: Docker
- **Orchestration**: Docker Compose
- **Web Server**: Nginx (for production frontend)

## Project Structure

```
.
├── backend/
│   ├── Controllers/
│   │   └── InsuranceController.cs    # Main API controller
│   ├── Models/
│   │   ├── InsuranceRequest.cs       # Request DTO
│   │   └── InsuranceResponse.cs      # Response DTO
│   ├── Program.cs                     # App configuration with CORS
│   ├── Dockerfile                     # Backend container config
│   └── backend.csproj
│
├── frontend/
│   ├── src/
│   │   ├── views/
│   │   │   └── HomeView.vue          # Main calculator UI
│   │   ├── App.vue                    # Root component
│   │   └── main.ts
│   ├── vite.config.ts                 # Vite config with proxy
│   ├── nginx.conf                     # Production web server config
│   ├── Dockerfile                     # Frontend container config
│   └── package.json
│
├── docker-compose.yml                 # Production: both services in Docker
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

## Environment Variables

### Backend
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: http://+:5000

### Frontend
- No environment variables required for basic setup
- API URL can be configured in Vite config

## Future Enhancements
- Add database for storing quotes
- Implement user authentication
- Add more NZ-specific risk factors (flood zones, earthquake zones)
- Email quote functionality
- Compare multiple insurance providers
- Add unit tests for calculation logic
- Add E2E tests for user flows
