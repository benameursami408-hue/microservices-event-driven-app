# Phase 5 - Test strategy and test files

This phase adds a practical QA layer for the SAV project. The goal is not to test every line immediately. The goal is to make the most important demo and correction scenarios repeatable.

## What was added

### Backend tests

New test projects:

- `src/services/AuthService/AuthService.Tests`
- `src/services/NotificationService/NotificationService.Tests`

Additional tests were added to existing test projects:

- `src/services/ReclamationService/ReclamationService.Tests`
- `src/services/InterventionService/InterventionService.Tests`

Coverage focus:

- Admin user create/update/delete rules
- duplicate email rejection
- password preservation during update
- outbox event publication after user creation
- notification workflow success/failure persistence
- role-based reclamation permissions
- planning and technician action policies

### Frontend tests

New frontend test setup:

- Vitest
- Testing Library
- jsdom
- Playwright

Unit tests were added for:

- admin user form validation
- enum/label helpers
- demo guide role helpers

E2E smoke tests were added for:

- redirecting anonymous users to login
- opening the guided test page after admin login
- blocking a too-short password before admin user creation

### Test scripts

Linux/macOS:

```bash
./tools/test-frontend.sh
./tools/test-backend.sh
./tools/test-all.sh
```

Windows PowerShell:

```powershell
./tools/test-frontend.ps1
./tools/test-backend.ps1
./tools/test-all.ps1
```

## Install dependencies

Frontend:

```bash
cd front
npm install
npx playwright install
```

Backend:

```bash
dotnet restore src/services/AuthService/AuthService.Tests/AuthService.Tests.csproj
dotnet restore src/services/ReclamationService/ReclamationService.Tests/ReclamationService.Tests.csproj
dotnet restore src/services/InterventionService/InterventionService.Tests/InterventionService.Tests.csproj
dotnet restore src/services/NotificationService/NotificationService.Tests/NotificationService.Tests.csproj
```

## Recommended local run order

1. Frontend static/unit verification:

```bash
cd front
npm run lint
npm run test
npm run build
```

2. Backend unit tests:

```bash
dotnet test src/services/AuthService/AuthService.Tests/AuthService.Tests.csproj
dotnet test src/services/ReclamationService/ReclamationService.Tests/ReclamationService.Tests.csproj
dotnet test src/services/InterventionService/InterventionService.Tests/InterventionService.Tests.csproj
dotnet test src/services/NotificationService/NotificationService.Tests/NotificationService.Tests.csproj
```

3. Full Docker stack smoke test:

```bash
docker compose up --build
```

4. E2E browser smoke test:

```bash
cd front
E2E_ADMIN_EMAIL=admin@sav.local E2E_ADMIN_PASSWORD='Admin123!' npm run e2e
```

## Manual demo checklist

Use this when preparing the encadrant meeting.

### Admin

- Login as admin.
- Open `/app/guide-test`.
- Open `/app/admin/users`.
- Create one SAV account.
- Create one ST account.
- Try a short password and verify the frontend blocks it.
- Try duplicate email and verify the backend rejects it clearly.
- Edit a user without entering a password and verify the update works.

### Client / ticket creation

- Login as client.
- Create a new reclamation.
- Verify reference, status, priority, and SLA are visible.
- Check notifications.

### SAV / ticket processing

- Login as SAV or admin.
- Open the created reclamation.
- Assign the ticket.
- Request planning.
- Verify history entries and allowed actions.

### Planning / intervention

- Open planning board.
- Assign a technician.
- Confirm appointment.
- Try a conflict scenario if available.
- Login as technician.
- Start intervention.
- Add diagnostic.
- Complete intervention.
- Publish report.

### Final verification

- Verify reclamation final status.
- Verify history timeline.
- Verify notifications.
- Verify dashboards still load.
- Verify mobile sidebar and guide page on a small screen.

## Notes

- The new tests are intentionally small and focused on high-risk flows.
- They are designed to protect the corrections from Phases 1-4.
- The E2E tests require the app and API to be runnable locally with a seeded admin account.
