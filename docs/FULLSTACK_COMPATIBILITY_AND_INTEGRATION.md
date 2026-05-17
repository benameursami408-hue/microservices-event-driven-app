# Full-stack compatibility and integration report

## Executive verdict

The backend and frontend are partially compatible after integration.

The backend exposes the main public API surface through Ocelot on `http://localhost:5005` for authentication, reclamations, planning, realisations/interventions and notifications. The frontend is now backend-driven and configured by:

```env
VITE_API_BASE_URL=http://localhost:5005
```

Core write operations for reclamations, planning appointments, interventions and notifications call backend APIs through the gateway. Legacy mock/localStorage flags are no longer part of the active frontend configuration.

## Architecture map

- Frontend: React/Vite SAV Pro UI. Design remains unchanged. The integration is isolated under `SAV-Pro/src/api` and small handler changes in pages.
- ApiGateway: Ocelot public entrypoint on port `5005`, routing to AuthService `5165`, ReclamationService `5057`, InterventionService `5104`, NotificationService `5242` in local development.
- AuthService: users, roles, passwords, JWT issuance, admin user management.
- ReclamationService: reclamation lifecycle, clients projection, priorities, SLA tracking, history and classification.
- InterventionService: planning requests, appointments, assignment, intervention realisation, diagnostics, parts, evidence and report publication.
- NotificationService: notification persistence, read/read-all/delete.
- Database: separate SQL Server databases per service in Docker/local config.
- Events: MassTransit/RabbitMQ integration events between services. Reclamation events drive planning/intervention projections and notification workflows.
- AI Priority module: rule-based backend endpoint plus frontend API fallback. No OpenAI key is exposed in React.

## Compatibility matrix

| Feature | Frontend status | Backend status | Compatible? | Action taken | Remaining issue |
|---|---|---|---|---|---|
| Auth login | Login page expects email/password and local session | `POST /api/auth/login` returns token + user | Yes | Added `authApi`, token storage and async login | Demo account emails differ from backend seeded emails |
| Auth register/current user | Register not used by current UI; current user read from session | `POST /api/auth/register`; no `/api/auth/me` | Partial | Mapper supports login/register | Add `/api/auth/me` later for token refresh/profile |
| Users & roles | UI uses `Admin`, `SAV`, `Technician`, `Client` | Backend uses `ADMIN`, `SAV`, `ST`, `CLIENT` | Yes with mapper | Added `roleMapper.js` and `usersApi.js` | Toggle user still uses mock unless adapted to call admin update |
| Clients | Clients page has local create/update | No controller originally | Partial | Added `GET/POST/PUT /api/clients` and gateway routes | `PATCH /api/clients/{id}/status` returns clear 400 because status belongs to AuthService |
| Reclamations list/create | UI uses reference strings as `id` | Backend uses numeric `Id` plus `Reference` | Yes with mapper | Added `reclamationsApi` and `reclamationMapper`; stores `technicalId` + `reference` | Some UI filters expect legacy product/client fields and are mapped best-effort |
| Reclamation assignment | UI assigns technician directly from reclamation page | Backend `/assign` assigns SAV; technician assignment is planning/intervention concern | Partial | Documented mismatch; API exposes `assignToSav` and `planReclamation` | Existing UI assign button keeps mock fallback unless a proper planning date is available |
| Planning request creation | UI creates local planning request | Backend supports `PATCH /api/reclamations/{id}/request-planning`; request consumed by InterventionService | Yes for backend rows | Wired request-planning call when `technicalId` exists | UI-created mock rows remain local until backend create succeeds |
| Appointment creation/confirmation/reschedule | UI creates local appointment | Backend supports `POST /api/planning/appointments`, confirm, reschedule, cancel | Yes with mapper | Added `planningApi` and handler integration | Requires backend `PlanningRequestId` GUID, so mock request rows cannot be posted |
| Technician assignment | UI assigns technician on appointment | Backend supports `POST /api/planning/appointments/{id}/assign-technician` | Yes for backend appointment rows | Wired API mode assignment | Frontend technician mock IDs may not match AuthService ST IDs unless seeded consistently |
| Intervention start/complete | UI uses `In Progress` and `Fixed` | Backend uses `Started` and `Solved` | Yes with mapper | Added `interventionsApi` and handler integration | Draft fields stay local unless backend has detail endpoints for diagnostics lists |
| Diagnostic / repair / parts / evidence | UI supports repair actions, parts, evidence | Backend supports diagnostic, repair-actions, parts-used, evidences | Partial | Repair action and part calls wired; mapper files added | Evidence UI currently mock-only; diagnostic UI not fully surfaced |
| Visit report draft/publish/list | UI lists local visit reports and publishes | Backend publishes through `POST /api/realisations/interventions/{id}/publish-report`; no list controller | Partial | Added `reportsApi` publish wrapper | `GET /api/visit-reports` remains missing and falls back to local reports |
| Notifications list/read/read-all/delete | UI supports list/read/read-all/delete | Backend had list/read/read-all; no delete | Yes | Added `DELETE /api/notifications/{id}` and frontend API wrapper | Create notification remains event-driven/backend-side, not direct UI API |
| Dashboard stats | UI computes from local db | Backend had `GET /api/reclamations/stats` for ADMIN only | Partial | Added `GET /api/dashboard/summary` via ReclamationService | Summary is reclamation-centric, not a cross-service aggregate yet |
| AI priority analysis | UI did not contain a dedicated AI component | No endpoint originally | Partial | Added `POST /api/ai/reclamations/analyze-priority` and frontend `aiApi` rule fallback | No Apply Suggestion UI was added to avoid changing approved visual design |

## Contract mismatches found

- Reclamation route naming: frontend expected plural `/api/reclamations`; backend controller class is `ReclamationsController` with `[Route("api/[controller]")]`, so the real route is `/api/reclamations`. Compatible.
- Role mismatch: frontend `Technician` maps to backend `ST`; frontend `Admin` maps to backend `ADMIN`.
- Priority mismatch: backend enum contains `MEDUIM`, not `MEDIUM`. The frontend mapper intentionally sends `MEDUIM` for `Medium`.
- Reclamation status mismatch: frontend displays `In Progress`; backend enum is `InProgress`.
- Intervention status mismatch: frontend displays `In Progress`; backend enum uses `Started`.
- Outcome mismatch: frontend uses `Fixed`; backend uses `Solved`.
- ID/reference mismatch: frontend historically used display references such as `REF-2024-0456` as `id`; backend uses numeric `Id` and separate `Reference`. Mappers now store both `id` and `technicalId`.
- Reclamation assignment mismatch: frontend assigns technicians from the reclamation details panel; backend `PATCH /api/reclamations/{id}/assign` assigns SAV ownership. Technician assignment belongs to planning appointments.
- Client status mismatch: frontend can conceptually display active/inactive users, but ReclamationService `Client` has no status column. Account status belongs to AuthService `User.IsActive`.
- Visit report list mismatch: frontend expects report list data; backend only has report publication through intervention realisation workflow.

## Changes implemented

### Frontend files changed/added

- `SAV-Pro/.env.example`
- `SAV-Pro/src/api/apiClient.js`
- `SAV-Pro/src/api/authApi.js`
- `SAV-Pro/src/api/usersApi.js`
- `SAV-Pro/src/api/clientsApi.js`
- `SAV-Pro/src/api/reclamationsApi.js`
- `SAV-Pro/src/api/planningApi.js`
- `SAV-Pro/src/api/interventionsApi.js`
- `SAV-Pro/src/api/notificationsApi.js`
- `SAV-Pro/src/api/reportsApi.js`
- `SAV-Pro/src/api/aiApi.js`
- `SAV-Pro/src/api/bootstrapApi.js`
- `SAV-Pro/src/api/mappers/roleMapper.js`
- `SAV-Pro/src/api/mappers/priorityMapper.js`
- `SAV-Pro/src/api/mappers/statusMapper.js`
- `SAV-Pro/src/api/mappers/userMapper.js`
- `SAV-Pro/src/api/mappers/clientMapper.js`
- `SAV-Pro/src/api/mappers/reclamationMapper.js`
- `SAV-Pro/src/api/mappers/planningMapper.js`
- `SAV-Pro/src/api/mappers/interventionMapper.js`
- `SAV-Pro/src/api/mappers/reportMapper.js`
- `SAV-Pro/src/services/authService.js`
- `SAV-Pro/src/App.jsx`
- `SAV-Pro/src/pages/LoginPage.jsx`
- `SAV-Pro/src/pages/ReclamationsPage.jsx`
- `SAV-Pro/src/pages/PlanningPage.jsx`
- `SAV-Pro/src/pages/InterventionsPage.jsx`
- `SAV-Pro/src/pages/NotificationsPage.jsx`

No CSS, layout, sidebar, topbar, card, badge, table, icon or visual structure files were changed.

### Backend files changed/added

- `src/services/ReclamationService/ReclamationService.Application/DTOs/ClientDtos.cs`
- `src/services/ReclamationService/ReclamationService.Application/DTOs/AiPriorityDtos.cs`
- `src/services/ReclamationService/ReclamationService.Application/Services/ClientsService.cs`
- `src/services/ReclamationService/ReclamationService.Application/Services/AiPriorityService.cs`
- `src/services/ReclamationService/ReclamationService.Domain/Interfaces/IClientRepository.cs`
- `src/services/ReclamationService/ReclamationService.Infrastructure/Repositories/ClientRepository.cs`
- `src/services/ReclamationService/ReclamationService.Api/Controllers/ClientsController.cs`
- `src/services/ReclamationService/ReclamationService.Api/Controllers/AiPriorityController.cs`
- `src/services/ReclamationService/ReclamationService.Api/Controllers/DashboardController.cs`
- `src/services/ReclamationService/ReclamationService.Api/Program.cs`
- `src/services/NotificationService/NotificationService.Domain/Interfaces/INotificationRepository.cs`
- `src/services/NotificationService/NotificationService.Infrastructure/Repositories/NotificationRepository.cs`
- `src/services/NotificationService/NotificationService.Api/Controllers/NotificationsController.cs`
- `src/gateway/ApiGateway/ocelot.Development.json`
- `src/gateway/ApiGateway/ocelot.json`

## API integration details

- Base URL: `VITE_API_BASE_URL`, defaulting to `http://localhost:5005`.
- Token handling: token is stored in `sav-pro-api-token-v1`; each API request sends `Authorization: Bearer <token>` when available.
- 401 handling: token/session are cleared and the UI redirects to `/login`.
- Error handling: `ApiError` includes `message`, `status`, `details` and `path`.
- Backend mode: business workflows use the gateway/backend contracts.
- Backend gateway: frontend points to ApiGateway, not individual services.

## AI Priority integration

- Backend endpoint: `POST /api/ai/reclamations/analyze-priority`.
- Frontend client: `SAV-Pro/src/api/aiApi.js`.
- Current implementation: deterministic/rule-based, not an OpenAI call.
- Security: no AI key or OpenAI call is made from the frontend.
- Switch to real AI later: replace `AiPriorityService.Analyze` internals with a backend-side provider and keep the same DTO contract.
- AI priority: the active implementation calls the backend rule-based endpoint and does not expose AI keys in the frontend.

## Runtime configuration

Expected local mode:

- Frontend: `http://localhost:5173`
- ApiGateway: `http://localhost:5005`
- AuthService: `http://localhost:5165`
- ReclamationService: `http://localhost:5057`
- InterventionService: `http://localhost:5104`
- NotificationService: `http://localhost:5242`
- SQL Server Docker port: `127.0.0.1:14333`
- RabbitMQ: `127.0.0.1:5672`, management `127.0.0.1:15672`

## Build/test results

Commands run:

```bash
cd SAV-Pro
npm install
npm run build
```

Frontend build result: passed.

The first build attempt failed because the uploaded `node_modules/.bin/vite` was not executable and the Rollup native optional package was missing from the extracted dependency tree. Reinstalling dependencies and ensuring Rollup optional dependency availability allowed the Vite build to pass.

```bash
dotnet restore
dotnet build
```

Backend build result: not executed in this environment because `dotnet` is not installed (`dotnet: command not found`). Backend changes were statically validated by inspecting controllers, DTOs, DI registration and Ocelot routes.

## Remaining limitations

- Backend `dotnet build` still needs to be run on a machine with the .NET SDK installed.
- Backend seeded users are documented in the root `README.md`: `admin@savpro.local`, `sav@savpro.local`, `tech1@savpro.local` to `tech4@savpro.local`, and `client1@savpro.local` to `client6@savpro.local`, all with `Password123!`.
- `GET /api/visit-reports` is still missing; visit report list remains mock/local unless a dedicated report controller is added.
- AI Apply Suggestion UI/history/notification workflow is not fully wired because there was no existing AI card/UI to preserve.
- Client status is not persisted in ReclamationService; active/inactive belongs to AuthService `User.IsActive`.
- Reclamation technician assignment remains a workflow mismatch; backend expects technician assignment through planning appointments.
- Dashboard summary is reclamation-centric and does not yet aggregate all services.

## Next recommended steps

1. Stabilize runtime with Docker Compose or local gateway mode, then verify every service health endpoint.
2. Run `dotnet restore` and `dotnet build` on all service solutions.
3. Align seed/demo users so frontend demo buttons match backend AuthService credentials.
4. Add a proper VisitReports read/list controller in InterventionService.
5. Replace remaining mock-only actions with API calls after endpoint coverage is complete.
6. Add automated frontend integration tests and backend controller/service tests for the new endpoints.
7. Extend AI Apply Suggestion backend workflow to update priority, history and notifications in one backend transaction.
