# SAV Pro Demo Data

Ce projet contient un seed de donnees de demonstration PFE pour les microservices Auth, Reclamation, Intervention et Notification.

## Activation

Le seed se lance automatiquement au demarrage des services quand les variables suivantes sont actives :

```env
SEED_ENABLED=true
SEED_DEMO_DATA=true
RESET_DEMO_DATA=false
ASPNETCORE_ENVIRONMENT=Development
```

Dans `docker-compose.yml`, ces valeurs sont mappees vers :

```env
Seed__Enabled=true
Seed__DemoData=true
Seed__ResetDemoData=false
```

Pour repartir d'une base vide et rejouer toutes les migrations + seed :

```bash
docker compose down -v
docker compose up --build
```

Pour supprimer uniquement les donnees de demo gerees par les seeders, en environnement de developpement uniquement :

```env
RESET_DEMO_DATA=true
```

Puis relancer les services. Remettre ensuite `RESET_DEMO_DATA=false` pour eviter de reset a chaque demarrage.

## Comptes de test

Tous les comptes utilisent le mot de passe :

```text
Password123!
```

| Role demo | Role backend reel | Email |
|---|---|---|
| Admin | `ADMIN` | `admin@savpro.local` |
| SAV Manager | `SAV` | `sav@savpro.local` |
| Technicien 1 | `ST` | `tech1@savpro.local` |
| Technicien 2 | `ST` | `tech2@savpro.local` |
| Technicien 3 | `ST` | `tech3@savpro.local` |
| Technicien 4 | `ST` | `tech4@savpro.local` |
| Client 1 | `CLIENT` | `client1@savpro.local` |
| Client 2 | `CLIENT` | `client2@savpro.local` |
| Client 3 | `CLIENT` | `client3@savpro.local` |
| Client 4 | `CLIENT` | `client4@savpro.local` |
| Client 5 | `CLIENT` | `client5@savpro.local` |
| Client 6 | `CLIENT` | `client6@savpro.local` |

> Le backend existant utilise `SAV` au lieu de `SAV_MANAGER`, et `ST` au lieu de `TECHNICIAN`. Le frontend accepte deja `ST` comme technicien.

## Donnees creees

### AuthService

- 12 comptes de demo.
- IDs stables pour synchronisation inter-services :
  - Admin : `100`
  - SAV : `200`
  - Techniciens : `301` a `304`
  - Clients : `501` a `506`
- Mots de passe hashes avec le `IPasswordHasher` existant.

### ReclamationService

- 6 clients coherents avec les users clients.
- 25 reclamations `REC-2026-0001` a `REC-2026-0025`.
- Repartition de statuts : nouvelles, assignees, planifiees, en cours, resolues, fermees et annulees.
- Priorites : `LOW`, `MEDUIM`, `HIGH`, `URGENT`.
- Produits/equipements : groupes electrogenes, compresseurs, climatisation, pompe, four industriel, machine textile, refrigerateur professionnel, onduleur, chaudiere et tableau electrique.
- Historique de reclamation avec creation, affectation, planification, intervention, resolution, cloture et application de suggestion IA.
- 8 analyses IA rule-based explicables dans `AiPriorityAnalyses`.

### InterventionService

- 20 interventions `INT-2026-0001` a `INT-2026-0020`.
- `tech1@savpro.local` possede au moins 8 interventions :
  - 2 aujourd'hui.
  - 2 demain.
  - 2 en cours.
  - 2 terminees.
- `tech2@savpro.local` possede des interventions planifiees, en cours et terminees.
- Planning requests, appointments, assignments, diagnostics, repair actions, pieces utilisees, preuves et rapports de visite.
- 10+ rapports de visite publies ou brouillons selon le statut.

### NotificationService

- 30+ notifications de demonstration.
- Notifications admin, SAV, techniciens et clients.
- `tech1@savpro.local` recoit au moins 8 notifications visibles.

## Scenarios de demo PFE

### 1. Admin dashboard

1. Se connecter avec `admin@savpro.local`.
2. Ouvrir le dashboard.
3. Verifier les reclamations urgentes, SLA a risque, interventions du jour, rapports soumis et notifications.

### 2. SAV traite une reclamation urgente

1. Se connecter avec `sav@savpro.local`.
2. Ouvrir `Reclamations`.
3. Selectionner `REC-2026-0001`.
4. Montrer la priorite urgente et l'analyse IA rule-based.
5. Verifier l'historique et la planification.

### 3. Technicien voit ses interventions

1. Se connecter avec `tech1@savpro.local`.
2. Ouvrir `Interventions`.
3. Verifier que la liste contient les missions assignees, y compris celles d'aujourd'hui, en cours et terminees.
4. Ouvrir le detail d'une intervention.

### 4. Technicien complete un rapport

1. Depuis `tech1@savpro.local`, ouvrir une intervention en cours.
2. Cliquer `Terminer / Rapport`.
3. Completer le resume et publier le rapport.
4. Verifier que le rapport apparait dans `Visit Reports`.

### 5. Client suit sa reclamation

1. Se connecter avec `client1@savpro.local`.
2. Ouvrir le portail client ou les reclamations client si disponible.
3. Verifier les reclamations liees, les notifications et l'historique.

### 6. IA suggere une priorite urgente

1. Ouvrir une reclamation avec description de type production bloquee / danger / arret.
2. Afficher l'analyse dans `AI Priority Analysis`.
3. Presenter l'assistant comme un modele rule-based explicable, pas comme un vrai LLM/OpenAI.

## Notes techniques

- Les seeders sont idempotents : ils s'appuient sur des emails, references, IDs stables ou `SourceEvent` uniques.
- Les migrations existantes ne sont pas remplacees.
- Aucun secret reel n'est ajoute.
- Les mots de passe de demo sont uniquement pour l'environnement Development/Demo.
