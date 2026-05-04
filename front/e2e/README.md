# Frontend E2E tests

These Playwright tests are intentionally small smoke scenarios for the SAV demo.
They verify that:

- unauthenticated users are redirected to login;
- an admin can open the guided test page;
- the admin user form blocks short passwords before sending a create request.

## Run locally

```bash
cd front
npm install
npx playwright install
npm run e2e
```

Set credentials when your seed admin differs from the defaults:

```bash
E2E_ADMIN_EMAIL=admin@example.com E2E_ADMIN_PASSWORD='Admin123!' npm run e2e
```
