# Phase 3 - UI/UX simplifiee pour les tests et la demo

Cette phase ne change pas la logique metier. Elle rend le projet plus facile a comprendre, tester et presenter.

## Changements appliques

- Ajout d une page `Guide de test` accessible depuis `/app/guide-test`.
- Ajout du lien `Guide de test` dans la navigation principale.
- Ajout d un panneau `Parcours conseille` sur la page d accueil selon le role connecte.
- Ajout de raccourcis de demo dans le dashboard Admin.
- Clarification des libelles visibles dans l interface.
- Correction de petites repetitions dans le dashboard Admin.
- Ajout d un fichier central `front/src/utils/demoGuide.js` pour garder le scenario de test au meme endroit.

## Scenario recommande

1. Admin: creer ou verifier les comptes SAV, ST et Client.
2. Client/SAV/Admin: creer une reclamation.
3. SAV/Admin: rechercher et ouvrir le ticket.
4. SAV/Admin: verifier la planification.
5. ST/Admin: executer l intervention.
6. Tous roles: verifier les notifications.

## Fichiers modifies ou ajoutes

- `front/src/pages/DemoGuidePage.jsx`
- `front/src/components/RoleNextSteps.jsx`
- `front/src/utils/demoGuide.js`
- `front/src/App.jsx`
- `front/src/layouts/AppLayout.jsx`
- `front/src/pages/DashboardPage.jsx`
- `front/src/pages/AdminDashboardPage.jsx`
- `front/src/utils/enums.js`

## Verification locale

Apres extraction:

```bash
cd front
npm ci
npm run build
npm run lint
```

Puis tester dans le navigateur:

- `/app`
- `/app/guide-test`
- `/app/admin`
- `/app/admin/users`
- `/app/reclamations`
