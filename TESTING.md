# SAV testing guide

The detailed Phase 5 QA plan is in `docs/PHASE5_TEST_STRATEGY.md`.

## Quick commands

Frontend:

```bash
cd front
npm install
npm run lint
npm run test
npm run build
```

Backend:

```bash
./tools/test-backend.sh
```

Everything:

```bash
./tools/test-all.sh
```

E2E browser smoke tests:

```bash
cd front
npx playwright install
E2E_ADMIN_EMAIL=admin@sav.local E2E_ADMIN_PASSWORD='Admin123!' npm run e2e
```


## Phase 6 reliability smoke checks

After `docker compose up --build`, verify readiness endpoints:

```bash
curl http://localhost:5001/health/ready
curl http://localhost:5002/health/ready
curl http://localhost:5003/health/ready
curl http://localhost:5004/health/ready
curl http://localhost:5005/health/ready
```

Verify correlation-id behavior:

```bash
curl -i -H "X-Correlation-ID: test-correlation-001" http://localhost:5005/health/ready
```

The response should include the same `X-Correlation-ID`.

Or run the Phase 6 helper script:

```bash
./tools/test-reliability.sh
```

Windows:

```powershell
./tools/test-reliability.ps1
```

## Security smoke tests

After starting the full stack, run:

```bash
./tools/test-security.sh
```

Windows:

```powershell
./tools/test-security.ps1
```
