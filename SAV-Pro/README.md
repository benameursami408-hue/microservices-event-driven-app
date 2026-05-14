# SAV Pro React/Vite Demo

Frontend React/Vite de demonstration pour une plateforme SAV/CRM operationnelle.
Les donnees sont persistees dans `localStorage` et le modele est aligne avec les
entites AuthService, ReclamationService, InterventionService et NotificationService.

## Run

```powershell
npm config set registry https://registry.npmjs.org/
npm install --registry=https://registry.npmjs.org/
npm run dev
npm run build
```

Open http://localhost:5173

## Demo accounts

- Admin: admin@sav.local / admin123
- SAV: sav@sav.local / sav123
- Technician: tech@sav.local / tech123
- Client: client@sav.local / client123

## Notes

The app stores data in localStorage. Use Settings > Reset demo data after replacing
an older build.

This repository is frontend-only. It is structured to be connected to real APIs,
but it does not include a backend implementation.
