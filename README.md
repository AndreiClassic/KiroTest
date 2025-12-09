# NZ House Insurance Calculator

A simple application to calculate house insurance premiums in New Zealand.

## Tech Stack
- Backend: .NET 9 Web API
- Frontend: Vue 3
- Docker: Multi-container setup

## Quick Start

### Production (Full Docker)
```bash
run-prod.bat
```
- Frontend: http://localhost:8080
- Backend: http://localhost:5000

### Development (Backend in Docker, Frontend local)
```bash
run-dev.bat
```
Then in another terminal:
```bash
cd frontend
npm install
npm run dev
```
- Frontend: http://localhost:5173 (with hot reload)
- Backend: http://localhost:5000

### Stop Everything
```bash
stop.bat
```
