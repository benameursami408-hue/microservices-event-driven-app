# PFE.NET Frontend

React + Vite frontend for the PFE.NET microservices backend.

## Prerequisites

- Node.js 18+ (recommended: latest LTS)
- Backend running via API Gateway: `http://localhost:5005`

## Configure

Create `front/.env.local` from `front/.env.example`: 

```
VITE_API_BASE_URL=http://localhost:5005
```

## Run

From repo root:

```
npm install --prefix front
npm run --prefix front dev
```

Vite dev server (default): `http://localhost:5173`

## Backend endpoints used (via Gateway)

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/reclamations`
- `GET /api/reclamations/query?page=1&pageSize=10&search=...`
- `POST /api/reclamations`
- `GET /api/reclamations/{id}`
- `PUT /api/reclamations/{id}`
- `DELETE /api/reclamations/{id}`
- `GET /api/notifications?take=50`
- `PATCH /api/notifications/{id}/read`
- `PATCH /api/notifications/read-all`

## Notes

- JWT is stored in `localStorage` under `auth_token`.
- Axios attaches `Authorization: Bearer <token>` automatically.
