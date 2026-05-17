# SAV Pro Frontend

Frontend React/Vite de SAV Pro. L application est backend-driven et consomme le gateway sur `http://localhost:5005`.

## Configuration

```powershell
Copy-Item .env.example .env
```

Variable disponible:

```env
VITE_API_BASE_URL=http://localhost:5005
```

Ne pas ajouter de cle AI/OpenAI dans le frontend. L assistant priorite est gere cote backend avec des regles deterministes.

## Commandes

```powershell
npm ci
npm run dev
npm run build
```

Ouvrir `http://localhost:5173`.

## Comptes demo

Tous les comptes seedes par AuthService utilisent `Password123!`.

- Admin: `admin@savpro.local`
- SAV: `sav@savpro.local`
- Technicien: `tech1@savpro.local`
- Client: `client1@savpro.local`
