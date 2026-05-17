# Demo Scenarios

## Scenario Admin

1. Login `admin@savpro.local` / `Password123!`.
2. Ouvrir Dashboard et verifier KPIs, reclamations et notifications.
3. Ouvrir Users & Roles: creation/modification/suppression disponibles.
4. Ouvrir Reclamations, selectionner une ligne, analyser la priorite et appliquer la suggestion.
5. Verifier l historique et les notifications.

## Scenario SAV

1. Login `sav@savpro.local` / `Password123!`.
2. Ouvrir Users & Roles: la page est en lecture seule.
3. Ouvrir Reclamations, traiter une reclamation ouverte.
4. Creer une demande planning.
5. Consulter Planning Requests.

## Scenario Technicien

1. Login `tech1@savpro.local` / `Password123!`.
2. Ouvrir Interventions.
3. Consulter les interventions assignees.
4. Verifier `Visit Reports` si des rapports sont seedes ou publies.

## Scenario Client

1. Login `client1@savpro.local` / `Password123!`.
2. Creer une reclamation depuis le parcours client si disponible.
3. Suivre le statut et les notifications liees a sa demande.

## Points PFE a montrer

- Gateway unique sur `localhost:5005`.
- Cookie/JWT partage entre frontend et services.
- Contrats enums en chaines, incluant compatibilite `MEDUIM`.
- AI Priority Assistant rule-based, explicable et backend-ready.
- Communication asynchrone via RabbitMQ et SharedEvents.
