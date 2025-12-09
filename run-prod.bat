@echo off
echo Starting full application in Docker...
docker-compose up --build

echo.
echo Application is running:
echo   Frontend: http://localhost:8080
echo   Backend:  http://localhost:5000
