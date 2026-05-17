# SAV Pro - PFE Microservices

SAV Pro est une application apres-vente composee d un frontend React/Vite, d un gateway Ocelot et de quatre services .NET: Auth, Reclamation, Intervention et Notification. Le frontend parle au gateway sur `http://localhost:5005`.

## Architecture courte

- `SAV-Pro/`: frontend React/Vite.
- `src/gateway/ApiGateway`: gateway public Ocelot.
- `src/services/AuthService`: login, cookie/JWT `sav_access_token`, users et roles.
- `src/services/ReclamationService`: reclamations, SLA, priorite rule-based AI-ready, clients.
- `src/services/InterventionService`: planning, appointments, interventions, visit reports.
- `src/services/NotificationService`: notifications operationnelles.
- `src/building-blocks/SharedEvents`: contrats d evenements MassTransit/RabbitMQ.

## Prerequis

- .NET SDK 8.
- Node.js 20+ et npm.
- Docker Desktop.

## Ports

- Gateway public: `5005`.
- Frontend dev: `5173`.
- AuthService: interne Docker `8080`, expose local `5001`.
- ReclamationService: interne Docker `8080`, expose local `5002`.
- NotificationService: interne Docker `8080`, expose local `5003`.
- InterventionService: interne Docker `8080`, expose local `5004`.
- SQL Server: `14333`.
- RabbitMQ: `5672`, management `15672`.

## Lancement Docker

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Le fichier `.env` reste local. Ne pas commit de secrets reels. Les valeurs `.env.example` sont uniquement des valeurs de developpement.

## Lancement frontend

```powershell
cd SAV-Pro
npm ci
npm run dev
```

Le frontend utilise `VITE_API_BASE_URL=http://localhost:5005` via `SAV-Pro/.env.example`. Il ne contient pas de cle AI/OpenAI.

## Build et tests

```powershell
dotnet restore SAV.sln
dotnet build SAV.sln -c Release
dotnet test SAV.sln
cd SAV-Pro
npm ci
npm run build
docker compose config
```

## Comptes demo seedes

Tous les comptes seedes utilisent le mot de passe `Password123!`.

- Admin: `admin@savpro.local`
- SAV: `sav@savpro.local`
- Techniciens: `tech1@savpro.local`, `tech2@savpro.local`, `tech3@savpro.local`, `tech4@savpro.local`
- Clients: `client1@savpro.local` a `client6@savpro.local`

## Scenario demo PFE

1. Demarrer Docker et le frontend.
2. Se connecter comme Admin.
3. Verifier `/api/auth/me`.
4. Lister les reclamations.
5. Creer une reclamation client.
6. Lancer l analyse de priorite rule-based.
7. Appliquer la suggestion AI.
8. Consulter l historique de la reclamation.
9. Verifier une notification si la priorite devient High/Urgent.
10. Creer une demande planning, puis verifier planning/intervention/rapport selon les donnees disponibles.
