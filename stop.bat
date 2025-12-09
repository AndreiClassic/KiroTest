@echo off
echo Stopping all containers...
docker-compose down
docker-compose -f docker-compose.dev.yml down
echo Done!
