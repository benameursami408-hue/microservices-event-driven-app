# Phase 4 - Refactorisation des gros fichiers

## Objectif

Cette phase reduit la complexite des fichiers les plus longs sans changer les routes, les DTO, les endpoints, les statuts ou les contrats API.

Le but est de rendre le projet plus lisible pour le developpement, les corrections et l'explication devant l'encadrant.

## Backend

### ReclamationService

Le fichier principal suivant etait trop volumineux:

```txt
src/services/ReclamationService/ReclamationService.Application/Services/ReclamationsService.cs
```

Il a ete transforme en classe partielle C#:

```txt
ReclamationsService.cs              -> constructeur + creation
ReclamationsService.Queries.cs      -> lecture, recherche, pagination, suppression simple
ReclamationsService.Workflow.cs     -> update, assign, plan, start, resolve, close, cancel, reject
ReclamationsService.Operations.cs   -> historique, priorite, SLA, recalcul, override
ReclamationsService.Helpers.cs      -> securite interne, mapping, etats derives, outbox helpers
```

Les methodes publiques existantes gardent les memes noms et signatures.

### InterventionService

Le service planning a aussi ete decoupe:

```txt
PlanningService.cs
PlanningService.Commands.cs
PlanningService.Helpers.cs
```

Le service realisation a ete decoupe:

```txt
RealisationService.cs
RealisationService.Workflow.cs
RealisationService.Helpers.cs
```

## Frontend

Le fichier suivant etait trop long et difficile a maintenir:

```txt
front/src/pages/ReclamationDetailPage.jsx
```

Les sections visuelles ont ete extraites dans:

```txt
front/src/components/reclamations/ReclamationDetailSections.jsx
```

Nouveaux composants:

```txt
ReclamationKpiGrid
ReclamationDetailForm
ReclamationPrioritySlaCard
ReclamationWorkflowCard
ReclamationHistoryCard
ReclamationSidePanel
```

La page conserve la logique principale:

- chargement API
- etats React
- soumission formulaire
- actions workflow
- navigation

Les composants extraits gerent surtout l'affichage.

## Ce qui n'a pas ete change

- Pas de changement d'URL API
- Pas de changement de DTO
- Pas de changement d'enum
- Pas de changement de base de donnees
- Pas de changement RabbitMQ
- Pas de changement de workflow metier

## Tests locaux recommandes

```bash
cd front
npm ci
npm run build
npm run lint
```

Puis cote backend:

```bash
dotnet build
```

Enfin refaire le scenario manuel du guide de test:

1. Admin cree les utilisateurs SAV/ST/CLIENT.
2. Client cree une reclamation.
3. SAV/Admin affecte et demande la planification.
4. SAV/Admin planifie un rendez-vous.
5. ST execute l'intervention.
6. SAV/Admin cloture la reclamation.
