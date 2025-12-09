@echo off
echo Starting backend in Docker...
docker-compose -f docker-compose.dev.yml up -d

echo.
echo Backend is running at http://localhost:5000
echo.
echo To run frontend in dev mode:
echo   cd frontend
echo   npm install
echo   npm run dev
echo.
echo Frontend will be at http://localhost:5173
