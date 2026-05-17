# Testing

## Validation automatique

Executer depuis la racine:

```powershell
dotnet restore SAV.sln
dotnet build SAV.sln -c Release
dotnet test SAV.sln
docker compose config
```

Executer le frontend:

```powershell
cd SAV-Pro
npm ci
npm run build
```

## Validation Docker

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Verifier ensuite:

```powershell
Invoke-WebRequest http://localhost:5005/health/ready
Invoke-WebRequest http://localhost:5001/health/ready
Invoke-WebRequest http://localhost:5002/health/ready
Invoke-WebRequest http://localhost:5003/health/ready
Invoke-WebRequest http://localhost:5004/health/ready
```

## Smoke test manuel

1. Login admin avec `admin@savpro.local` / `Password123!`.
2. Appeler `GET /api/auth/me`.
3. Lister `GET /api/reclamations`.
4. Creer une reclamation client via UI ou `POST /api/reclamations`.
5. Appeler `POST /api/ai/reclamations/analyze-priority`.
6. Appliquer avec `POST /api/reclamations/{id}/ai-priority/apply`.
7. Verifier `GET /api/reclamations/{id}/history`.
8. Verifier `GET /api/notifications` pour High/Urgent.
9. Creer une demande planning depuis la reclamation.
10. Verifier `GET /api/planning/requests` puis `GET /api/visit-reports/mine` si un rapport existe.
