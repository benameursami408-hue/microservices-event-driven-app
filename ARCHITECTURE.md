# Architecture

## Vue d ensemble

SAV Pro suit une architecture microservices pragmatique pour le PFE. Le frontend React/Vite consomme uniquement le gateway Ocelot. Le gateway route ensuite vers les services .NET selon le domaine: Auth, Reclamation, Notification et Intervention.

## Flux principaux

- AuthService emet le JWT et le cookie HttpOnly `sav_access_token`.
- ReclamationService gere les reclamations, les clients, les priorites, le SLA et l assistant AI rule-based.
- InterventionService consomme les demandes planning et expose planning, rendez-vous, interventions et visit reports.
- NotificationService consomme les evenements metier et expose les notifications.
- SharedEvents contient les contrats MassTransit/RabbitMQ partages.

## Principes conserves

- Le frontend n expose aucune cle AI/OpenAI.
- L assistant priorite reste deterministe, rule-based et pret pour un backend AI plus tard.
- L enum `NamePriority.MEDUIM` est conservee pour compatibilite base/demo; le frontend mappe `Medium` vers `MEDUIM`.
- Le gateway public reste `http://localhost:5005`.
- En Docker, les services parlent en HTTP interne sur `8080`; la redirection HTTPS est desactivee en environnement `Docker`.

## Securite et roles

- ADMIN: acces complet users et back-office.
- SAV: lecture users, gestion operationnelle SAV, pas de create/edit/delete users.
- ST/TECHNICIAN: acces interventions/rapports selon les endpoints autorises.
- CLIENT: creation et consultation de ses reclamations.
