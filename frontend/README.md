# PFE.NET Frontend

React + Vite frontend for the PFE.NET microservices backend.

## Prerequisites

- Node.js 18+ (recommended: latest LTS)
- Backend running via API Gateway: `http://localhost:5000`

## Configure

Create `frontend/.env` (optional):

```
VITE_API_BASE_URL=http://localhost:5000
```

## Run

From repo root:

```
npm install --prefix frontend
npm run --prefix frontend dev
```

Vite dev server (default): `http://localhost:5173`

## Backend endpoints used (via Gateway)

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/reclamations`
- `POST /api/reclamations`
- `GET /api/reclamations/{id}`
- `PUT /api/reclamations/{id}`
- `DELETE /api/reclamations/{id}`
- `GET /api/notifications?take=50`

## Notes

- JWT is stored in `localStorage` under `auth_token`.
- Axios attaches `Authorization: Bearer <token>` automatically.
