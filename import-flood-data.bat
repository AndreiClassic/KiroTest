@echo off
echo ========================================
echo Auckland Flood Data Import Script
echo ========================================
echo.

REM Check if GeoJSON file path is provided
if "%~1"=="" (
    echo Usage: import-flood-data.bat "path\to\auckland_flood.geojson"
    echo.
    echo Example: import-flood-data.bat "C:\Downloads\auckland_flood.geojson"
    exit /b 1
)

set GEOJSON_FILE=%~1

REM Check if file exists
if not exist "%GEOJSON_FILE%" (
    echo ERROR: File not found: %GEOJSON_FILE%
    exit /b 1
)

echo Step 1: Copying GeoJSON file to backend container...
docker cp "%GEOJSON_FILE%" insurance-backend:/app/auckland_flood.geojson
if errorlevel 1 (
    echo ERROR: Failed to copy file to container
    echo Make sure Docker containers are running: docker-compose up -d
    exit /b 1
)
echo ✓ File copied successfully
echo.

echo Step 2: Initializing PostGIS database...
curl -X POST http://localhost:5000/api/floodadmin/initialize
echo.
echo ✓ Database initialized
echo.

echo Step 3: Importing flood data into PostGIS...
curl -X POST http://localhost:5000/api/floodadmin/import-auckland ^
  -H "Content-Type: application/json" ^
  -d "{\"filePath\": \"/app/auckland_flood.geojson\"}"
echo.
echo.

echo ========================================
echo Import Complete!
echo ========================================
echo.
echo You can now test the flood zone queries:
echo   curl http://localhost:5000/api/lot/1156
echo.
echo Or check the data in PostGIS:
echo   docker exec -it insurance-postgis psql -U postgres -d floodmaps
echo   SELECT COUNT(*) FROM auckland_flood_zones;
echo.
