# API Contract

Base URL frontend: `http://localhost:5005`.

## AuthService

- `POST /api/auth/login`: email/password, retourne le user et pose le cookie `sav_access_token`.
- `GET /api/auth/me`: retourne l utilisateur courant.
- `POST /api/auth/logout`: supprime le cookie.
- `GET /api/admin/users`: ADMIN et SAV.
- `POST /api/admin/users`: ADMIN seulement.
- `PUT /api/admin/users/{id}`: ADMIN seulement.
- `DELETE /api/admin/users/{id}`: ADMIN seulement.

Roles JSON attendus: `CLIENT`, `SAV`, `ADMIN`, `ST`.

## ReclamationService

- `GET /api/reclamations`: liste visible selon le role.
- `POST /api/reclamations`: creation. ADMIN/SAV doivent envoyer `clientId`; CLIENT utilise son id courant.
- `GET /api/reclamations/{id}`: detail visible.
- `PUT /api/reclamations/{id}`: mise a jour autorisee selon role/statut.
- `DELETE /api/reclamations/{id}`: ADMIN, reclamation Open/Cancelled.
- `GET /api/reclamations/{id}/history`: historique.
- `GET /api/reclamations/query`: recherche paginee par querystring.
- `POST /api/reclamations/query`: recherche paginee par body.
- `POST /api/reclamations/{id}/override-priority`: override manuel.
- `POST /api/ai/reclamations/analyze-priority`: analyse rule-based backend.
- `POST /api/reclamations/{id}/ai-priority/apply`: applique la derniere analyse ou `analysisId` du body.
- `POST /api/reclamations/{id}/ai-priority-analysis/{analysisId}/apply`: route historique conservee.
- `PATCH /api/reclamations/{id}/assign`: assignation SAV.
- `PATCH /api/reclamations/{id}/request-planning`: emission demande planning.

Priorites JSON: `LOW`, `MEDUIM`, `HIGH`, `URGENT`. `MEDUIM` est volontairement conserve pour compatibilite. Le frontend affiche `Medium`.

Statuts reclamation: `Open`, `Assigned`, `Planned`, `InProgress`, `Resolved`, `Closed`, `Cancelled`, `Rejected`.

## NotificationService

- `GET /api/notifications`: liste notifications.
- `GET /api/notifications/latest`: dernieres notifications.
- `PATCH /api/notifications/{id}/read`: marquer lu.
- `PATCH /api/notifications/read-all`: tout marquer lu.

## InterventionService

- `GET /api/planning/requests`: demandes planning visibles.
- `GET /api/planning/appointments`: rendez-vous.
- `POST /api/planning/appointments`: creation rendez-vous SAV/ADMIN.
- `POST /api/planning/appointments/{id}/assign-technician`: affectation technicien.
- `GET /api/visit-reports/mine`: rapports visibles pour l utilisateur courant.
- `GET /api/visit-reports`: liste back-office.

Le frontend envoie les enums sous forme de chaines. Les services .NET utilisent `JsonStringEnumConverter` pour uniformiser serialization/deserialization.
