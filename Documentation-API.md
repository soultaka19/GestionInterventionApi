# Documentation API - Gestion des Interventions

## Table des matières

1. [Introduction](#introduction)
2. [Authentification](#authentification)
3. [Cas d'utilisation](#cas-dutilisation)
4. [Endpoints API](#endpoints-api)
   - [Authentification](#authentification-api)
   - [Clients](#clients-api)
   - [Équipements](#équipements-api)
   - [Interventions](#interventions-api)
   - [Géolocalisation](#géolocalisation-api)
5. [Codes d'erreur](#codes-derreur)

---

## Introduction

L'API Gestion des Interventions permet de gérer les interventions techniques pour les entreprises de maintenance (chauffage, climatisation, plomberie, etc.). Elle offre une solution complète pour :

- Gérer les clients et leurs équipements
- Planifier et suivre les interventions
- Localiser les techniciens en temps réel
- Optimiser les itinéraires de tournée

**URL de base** : `https://api.gestion-intervention.com/api`

**Format des données** : JSON

**Authentification** : JWT Bearer Token

---

## Authentification

Toutes les requêtes (sauf `/api/auth/login` et `/api/auth/register`) nécessitent un token JWT dans l'en-tête :

```
Authorization: Bearer <votre_token_jwt>
```

---

## Cas d'utilisation

### 1. Gestion des Clients

| Cas d'utilisation | Description |
|-------------------|-------------|
| Créer un client | Un planificateur ajoute un nouveau client avec ses coordonnées. L'adresse est automatiquement géocodée pour obtenir les coordonnées GPS. |
| Consulter les clients | Afficher la liste des clients avec possibilité de recherche par nom ou adresse. |
| Modifier un client | Mettre à jour les informations d'un client (adresse, téléphone, email). |
| Supprimer un client | Retirer un client de la base de données. |

### 2. Gestion des Équipements

| Cas d'utilisation | Description |
|-------------------|-------------|
| Ajouter un équipement | Enregistrer un nouvel équipement (chaudière, climatisation, etc.) chez un client. |
| Suivre les garanties | Identifier les équipements dont la garantie expire bientôt. |
| Planifier la maintenance | Lister les équipements nécessitant une maintenance (selon la dernière date de maintenance). |
| Consulter l'historique | Voir tous les équipements d'un client avec leur historique de maintenance. |

### 3. Gestion des Interventions

| Cas d'utilisation | Description |
|-------------------|-------------|
| Créer une intervention | Un planificateur crée une demande d'intervention pour un client. |
| Affecter un technicien | Assigner un technicien à une intervention et définir le créneau horaire. |
| Démarrer l'intervention | Le technicien signale le début de son intervention sur le terrain. |
| Compléter l'intervention | Le technicien termine l'intervention avec un rapport détaillé (photos, signature client, pièces utilisées). |
| Annuler une intervention | Annuler une intervention planifiée avec motif. |
| Consulter le planning | Voir toutes les interventions sur une période donnée. |

### 4. Géolocalisation

| Cas d'utilisation | Description |
|-------------------|-------------|
| Mettre à jour la position | Le technicien envoie sa position GPS en temps réel. |
| Localiser les techniciens | Le planificateur visualise la position de tous les techniciens. |
| Optimiser un itinéraire | Calculer le meilleur trajet pour visiter plusieurs clients. |
| Calculer une distance | Estimer le temps de trajet entre deux points. |

---

## Endpoints API

---

### Authentification API

#### POST /api/auth/login

Authentifie un utilisateur et retourne un token JWT.

**Requête :**
```json
{
  "email": "jean.dupont@entreprise.fr",
  "password": "MotDePasse123!"
}
```

**Réponse (200 OK) :**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkw...",
  "expiresAt": "2026-02-04T10:30:00Z",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "jean.dupont@entreprise.fr",
    "firstName": "Jean",
    "lastName": "Dupont",
    "role": "Planificateur",
    "organizationId": "a1b2c3d4-5678-90ab-cdef-1234567890ab"
  }
}
```

**Erreurs possibles :**
| Code | Description |
|------|-------------|
| 401 | Email ou mot de passe incorrect |

---

#### POST /api/auth/register

Crée une nouvelle organisation avec un utilisateur administrateur.

**Requête :**
```json
{
  "organizationName": "Chauffage Express SARL",
  "email": "admin@chauffage-express.fr",
  "password": "SecurePassword456!",
  "firstName": "Marie",
  "lastName": "Martin"
}
```

**Réponse (201 Created) :**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-04T10:30:00Z",
  "user": {
    "id": "b2c3d4e5-6789-01ab-cdef-234567890abc",
    "email": "admin@chauffage-express.fr",
    "firstName": "Marie",
    "lastName": "Martin",
    "role": "Admin",
    "organizationId": "c3d4e5f6-7890-12bc-def0-345678901bcd"
  }
}
```

**Erreurs possibles :**
| Code | Description |
|------|-------------|
| 400 | Données invalides ou email déjà utilisé |

---

### Clients API

#### GET /api/clients

Récupère la liste des clients de l'organisation.

**Paramètres de requête :**
| Paramètre | Type | Description |
|-----------|------|-------------|
| search | string (optionnel) | Recherche par nom ou adresse |

**Exemple :** `GET /api/clients?search=dupont`

**Réponse (200 OK) :**
```json
[
  {
    "id": "d4e5f6a7-8901-23cd-ef01-456789012def",
    "name": "Famille Dupont",
    "address": "15 Rue de la Paix",
    "city": "Paris",
    "postalCode": "75002",
    "phone": "01 23 45 67 89",
    "email": "dupont@email.fr",
    "latitude": 48.8698,
    "longitude": 2.3311,
    "notes": "Code portail: 1234",
    "equipmentCount": 2,
    "createdAt": "2025-06-15T09:00:00Z"
  },
  {
    "id": "e5f6a7b8-9012-34de-f012-567890123ef0",
    "name": "Restaurant Le Gourmet",
    "address": "42 Avenue des Champs-Élysées",
    "city": "Paris",
    "postalCode": "75008",
    "phone": "01 98 76 54 32",
    "email": "contact@legourmet.fr",
    "latitude": 48.8738,
    "longitude": 2.2950,
    "notes": "Accès par l'arrière-cuisine",
    "equipmentCount": 5,
    "createdAt": "2025-08-22T14:30:00Z"
  }
]
```

---

#### GET /api/clients/{id}

Récupère les détails d'un client spécifique.

**Exemple :** `GET /api/clients/d4e5f6a7-8901-23cd-ef01-456789012def`

**Réponse (200 OK) :**
```json
{
  "id": "d4e5f6a7-8901-23cd-ef01-456789012def",
  "name": "Famille Dupont",
  "address": "15 Rue de la Paix",
  "city": "Paris",
  "postalCode": "75002",
  "phone": "01 23 45 67 89",
  "email": "dupont@email.fr",
  "latitude": 48.8698,
  "longitude": 2.3311,
  "notes": "Code portail: 1234",
  "equipmentCount": 2,
  "createdAt": "2025-06-15T09:00:00Z"
}
```

**Erreurs possibles :**
| Code | Description |
|------|-------------|
| 404 | Client non trouvé |

---

#### POST /api/clients

Crée un nouveau client. L'adresse est automatiquement géocodée.

**Requête :**
```json
{
  "name": "Boulangerie Au Pain Doré",
  "address": "8 Place du Marché",
  "city": "Lyon",
  "postalCode": "69001",
  "phone": "04 78 12 34 56",
  "email": "contact@painedore.fr",
  "notes": "Intervention préférable avant 6h du matin"
}
```

**Réponse (201 Created) :**
```json
{
  "id": "f6a7b8c9-0123-45ef-0123-678901234f01",
  "name": "Boulangerie Au Pain Doré",
  "address": "8 Place du Marché",
  "city": "Lyon",
  "postalCode": "69001",
  "phone": "04 78 12 34 56",
  "email": "contact@painedore.fr",
  "latitude": 45.7676,
  "longitude": 4.8344,
  "notes": "Intervention préférable avant 6h du matin",
  "equipmentCount": 0,
  "createdAt": "2026-02-03T08:15:00Z"
}
```

**Erreurs possibles :**
| Code | Description |
|------|-------------|
| 400 | Données invalides (nom ou adresse manquant) |

---

#### PUT /api/clients/{id}

Met à jour les informations d'un client.

**Exemple :** `PUT /api/clients/d4e5f6a7-8901-23cd-ef01-456789012def`

**Requête :**
```json
{
  "name": "Famille Dupont-Martin",
  "address": "15 Rue de la Paix",
  "city": "Paris",
  "postalCode": "75002",
  "phone": "01 23 45 67 00",
  "email": "dupont.martin@email.fr",
  "notes": "Nouveau code portail: 5678"
}
```

**Réponse (200 OK) :**
```json
{
  "id": "d4e5f6a7-8901-23cd-ef01-456789012def",
  "name": "Famille Dupont-Martin",
  "address": "15 Rue de la Paix",
  "city": "Paris",
  "postalCode": "75002",
  "phone": "01 23 45 67 00",
  "email": "dupont.martin@email.fr",
  "latitude": 48.8698,
  "longitude": 2.3311,
  "notes": "Nouveau code portail: 5678",
  "equipmentCount": 2,
  "createdAt": "2025-06-15T09:00:00Z"
}
```

---

#### DELETE /api/clients/{id}

Supprime un client.

**Exemple :** `DELETE /api/clients/d4e5f6a7-8901-23cd-ef01-456789012def`

**Réponse (204 No Content)** : Pas de corps de réponse

**Erreurs possibles :**
| Code | Description |
|------|-------------|
| 404 | Client non trouvé |

---

#### GET /api/clients/{id}/equipments

Récupère tous les équipements d'un client.

**Exemple :** `GET /api/clients/d4e5f6a7-8901-23cd-ef01-456789012def/equipments`

**Réponse (200 OK) :**
```json
[
  {
    "id": "a1b2c3d4-5678-90ab-cdef-111111111111",
    "clientId": "d4e5f6a7-8901-23cd-ef01-456789012def",
    "clientName": "Famille Dupont",
    "type": "Chaudiere",
    "brand": "Viessmann",
    "model": "Vitodens 200-W",
    "serialNumber": "VD200W-2024-78542",
    "installationDate": "2024-03-15",
    "lastMaintenanceDate": "2025-03-15",
    "warrantyEndDate": "2027-03-15",
    "notes": "Chaudière gaz condensation 26kW",
    "createdAt": "2024-03-15T10:00:00Z"
  },
  {
    "id": "b2c3d4e5-6789-01ab-cdef-222222222222",
    "clientId": "d4e5f6a7-8901-23cd-ef01-456789012def",
    "clientName": "Famille Dupont",
    "type": "ChauffeEau",
    "brand": "Atlantic",
    "model": "Zeneo 200L",
    "serialNumber": "ATL-ZN200-2023-45678",
    "installationDate": "2023-09-20",
    "lastMaintenanceDate": null,
    "warrantyEndDate": "2028-09-20",
    "notes": "Ballon thermodynamique",
    "createdAt": "2024-03-15T10:30:00Z"
  }
]
```

---

### Équipements API

#### GET /api/equipments

Récupère la liste des équipements avec filtres optionnels.

**Paramètres de requête :**
| Paramètre | Type | Description |
|-----------|------|-------------|
| clientId | GUID (optionnel) | Filtrer par client |
| type | string (optionnel) | Type d'équipement (voir types disponibles) |
| search | string (optionnel) | Recherche par marque, modèle ou numéro de série |

**Types d'équipements disponibles :**
- `Chaudiere`
- `Radiateur`
- `Climatisation`
- `PompeAChaleur`
- `ChauffeEau`
- `Ventilation`
- `Autre`

**Exemple :** `GET /api/equipments?type=Chaudiere&search=viessmann`

**Réponse (200 OK) :**
```json
[
  {
    "id": "a1b2c3d4-5678-90ab-cdef-111111111111",
    "clientId": "d4e5f6a7-8901-23cd-ef01-456789012def",
    "clientName": "Famille Dupont",
    "type": "Chaudiere",
    "brand": "Viessmann",
    "model": "Vitodens 200-W",
    "serialNumber": "VD200W-2024-78542",
    "installationDate": "2024-03-15",
    "lastMaintenanceDate": "2025-03-15",
    "warrantyEndDate": "2027-03-15",
    "notes": "Chaudière gaz condensation 26kW",
    "createdAt": "2024-03-15T10:00:00Z"
  }
]
```

---

#### GET /api/equipments/{id}

Récupère les détails d'un équipement.

**Exemple :** `GET /api/equipments/a1b2c3d4-5678-90ab-cdef-111111111111`

**Réponse (200 OK) :**
```json
{
  "id": "a1b2c3d4-5678-90ab-cdef-111111111111",
  "clientId": "d4e5f6a7-8901-23cd-ef01-456789012def",
  "clientName": "Famille Dupont",
  "type": "Chaudiere",
  "brand": "Viessmann",
  "model": "Vitodens 200-W",
  "serialNumber": "VD200W-2024-78542",
  "installationDate": "2024-03-15",
  "lastMaintenanceDate": "2025-03-15",
  "warrantyEndDate": "2027-03-15",
  "notes": "Chaudière gaz condensation 26kW",
  "createdAt": "2024-03-15T10:00:00Z"
}
```

---

#### POST /api/equipments

Crée un nouvel équipement.

**Requête :**
```json
{
  "clientId": "d4e5f6a7-8901-23cd-ef01-456789012def",
  "type": "Climatisation",
  "brand": "Daikin",
  "model": "FTXM35R",
  "serialNumber": "DAI-FTXM35-2026-12345",
  "installationDate": "2026-02-01",
  "warrantyEndDate": "2031-02-01",
  "notes": "Split mural 3.5kW - Chambre principale"
}
```

**Réponse (201 Created) :**
```json
{
  "id": "c3d4e5f6-7890-12ab-cdef-333333333333",
  "clientId": "d4e5f6a7-8901-23cd-ef01-456789012def",
  "clientName": "Famille Dupont",
  "type": "Climatisation",
  "brand": "Daikin",
  "model": "FTXM35R",
  "serialNumber": "DAI-FTXM35-2026-12345",
  "installationDate": "2026-02-01",
  "lastMaintenanceDate": null,
  "warrantyEndDate": "2031-02-01",
  "notes": "Split mural 3.5kW - Chambre principale",
  "createdAt": "2026-02-03T09:00:00Z"
}
```

---

#### PUT /api/equipments/{id}

Met à jour un équipement.

**Exemple :** `PUT /api/equipments/a1b2c3d4-5678-90ab-cdef-111111111111`

**Requête :**
```json
{
  "type": "Chaudiere",
  "brand": "Viessmann",
  "model": "Vitodens 200-W",
  "serialNumber": "VD200W-2024-78542",
  "installationDate": "2024-03-15",
  "lastMaintenanceDate": "2026-01-20",
  "warrantyEndDate": "2027-03-15",
  "notes": "Chaudière gaz condensation 26kW - Entretien annuel effectué"
}
```

**Réponse (200 OK) :** Équipement mis à jour (même format que GET)

---

#### DELETE /api/equipments/{id}

Supprime un équipement.

**Exemple :** `DELETE /api/equipments/c3d4e5f6-7890-12ab-cdef-333333333333`

**Réponse (204 No Content)**

---

#### PATCH /api/equipments/{id}/maintenance

Marque l'équipement comme maintenancé (met à jour `lastMaintenanceDate` à maintenant).

**Exemple :** `PATCH /api/equipments/a1b2c3d4-5678-90ab-cdef-111111111111/maintenance`

**Réponse (200 OK) :**
```json
{
  "id": "a1b2c3d4-5678-90ab-cdef-111111111111",
  "lastMaintenanceDate": "2026-02-03T09:30:00Z",
  "...": "autres champs"
}
```

---

#### GET /api/equipments/types

Récupère la liste des types d'équipements.

**Réponse (200 OK) :**
```json
[
  { "value": 0, "name": "Chaudiere" },
  { "value": 1, "name": "Radiateur" },
  { "value": 2, "name": "Climatisation" },
  { "value": 3, "name": "PompeAChaleur" },
  { "value": 4, "name": "ChauffeEau" },
  { "value": 5, "name": "Ventilation" },
  { "value": 6, "name": "Autre" }
]
```

---

#### GET /api/equipments/warranty-expiring

Récupère les équipements dont la garantie expire bientôt.

**Paramètres de requête :**
| Paramètre | Type | Défaut | Description |
|-----------|------|--------|-------------|
| daysAhead | int | 30 | Nombre de jours à l'avance |

**Exemple :** `GET /api/equipments/warranty-expiring?daysAhead=60`

**Réponse (200 OK) :** Liste d'équipements (même format que GET /api/equipments)

---

#### GET /api/equipments/maintenance-due

Récupère les équipements nécessitant une maintenance.

**Paramètres de requête :**
| Paramètre | Type | Défaut | Description |
|-----------|------|--------|-------------|
| monthsSinceLastMaintenance | int | 12 | Mois depuis la dernière maintenance |

**Exemple :** `GET /api/equipments/maintenance-due?monthsSinceLastMaintenance=6`

**Réponse (200 OK) :** Liste d'équipements nécessitant une maintenance

---

### Interventions API

#### GET /api/interventions

Récupère la liste des interventions avec filtres.

**Paramètres de requête :**
| Paramètre | Type | Description |
|-----------|------|-------------|
| status | string | Statut de l'intervention |
| technicianId | GUID | Filtrer par technicien |
| clientId | GUID | Filtrer par client |
| fromDate | DateTime | Date de début |
| toDate | DateTime | Date de fin |

**Statuts d'intervention :**
- `Pending` - En attente d'affectation
- `Scheduled` - Planifiée
- `InProgress` - En cours
- `Completed` - Terminée
- `Cancelled` - Annulée

**Types d'intervention :**
- `Maintenance` - Entretien régulier
- `Repair` - Réparation
- `Installation` - Installation
- `Inspection` - Inspection/Diagnostic
- `Emergency` - Urgence

**Exemple :** `GET /api/interventions?status=Scheduled&fromDate=2026-02-03&toDate=2026-02-10`

**Réponse (200 OK) :**
```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "clientId": "d4e5f6a7-8901-23cd-ef01-456789012def",
    "clientName": "Famille Dupont",
    "clientAddress": "15 Rue de la Paix, 75002 Paris",
    "equipmentId": "a1b2c3d4-5678-90ab-cdef-111111111111",
    "equipmentInfo": "Chaudiere - Viessmann Vitodens 200-W",
    "technicianId": "tech-1111-2222-3333-444444444444",
    "technicianName": "Pierre Leroy",
    "type": "Maintenance",
    "status": "Scheduled",
    "description": "Entretien annuel chaudière gaz",
    "scheduledDate": "2026-02-05",
    "scheduledStartTime": "09:00:00",
    "scheduledEndTime": "10:30:00",
    "estimatedDurationMinutes": 90,
    "startedAt": null,
    "completedAt": null,
    "notes": "Client disponible le matin uniquement",
    "createdAt": "2026-01-28T14:00:00Z"
  }
]
```

---

#### GET /api/interventions/{id}

Récupère les détails complets d'une intervention.

**Exemple :** `GET /api/interventions/11111111-1111-1111-1111-111111111111`

**Réponse (200 OK) :**
```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "clientId": "d4e5f6a7-8901-23cd-ef01-456789012def",
  "clientName": "Famille Dupont",
  "clientAddress": "15 Rue de la Paix, 75002 Paris",
  "clientPhone": "01 23 45 67 89",
  "clientLatitude": 48.8698,
  "clientLongitude": 2.3311,
  "equipmentId": "a1b2c3d4-5678-90ab-cdef-111111111111",
  "equipmentType": "Chaudiere",
  "equipmentBrand": "Viessmann",
  "equipmentModel": "Vitodens 200-W",
  "equipmentInfo": "Chaudiere - Viessmann Vitodens 200-W",
  "technicianId": "tech-1111-2222-3333-444444444444",
  "technicianName": "Pierre Leroy",
  "technicianEmail": "pierre.leroy@entreprise.fr",
  "type": "Maintenance",
  "status": "Scheduled",
  "description": "Entretien annuel chaudière gaz",
  "scheduledDate": "2026-02-05",
  "scheduledStartTime": "09:00:00",
  "scheduledEndTime": "10:30:00",
  "estimatedDurationMinutes": 90,
  "startedAt": null,
  "completedAt": null,
  "notes": "Client disponible le matin uniquement",
  "technicianNotes": null,
  "report": null,
  "createdAt": "2026-01-28T14:00:00Z",
  "updatedAt": null
}
```

---

#### POST /api/interventions

Crée une nouvelle intervention.

**Requête :**
```json
{
  "clientId": "d4e5f6a7-8901-23cd-ef01-456789012def",
  "equipmentId": "a1b2c3d4-5678-90ab-cdef-111111111111",
  "type": "Repair",
  "description": "Fuite d'eau au niveau du circulateur",
  "notes": "Client signale une flaque d'eau sous la chaudière depuis ce matin"
}
```

**Réponse (201 Created) :**
```json
{
  "id": "22222222-2222-2222-2222-222222222222",
  "clientId": "d4e5f6a7-8901-23cd-ef01-456789012def",
  "clientName": "Famille Dupont",
  "clientAddress": "15 Rue de la Paix, 75002 Paris",
  "equipmentId": "a1b2c3d4-5678-90ab-cdef-111111111111",
  "equipmentInfo": "Chaudiere - Viessmann Vitodens 200-W",
  "technicianId": null,
  "technicianName": null,
  "type": "Repair",
  "status": "Pending",
  "description": "Fuite d'eau au niveau du circulateur",
  "scheduledDate": null,
  "scheduledStartTime": null,
  "scheduledEndTime": null,
  "estimatedDurationMinutes": null,
  "startedAt": null,
  "completedAt": null,
  "notes": "Client signale une flaque d'eau sous la chaudière depuis ce matin",
  "createdAt": "2026-02-03T10:00:00Z"
}
```

---

#### PUT /api/interventions/{id}

Met à jour une intervention (impossible si terminée ou annulée).

**Exemple :** `PUT /api/interventions/22222222-2222-2222-2222-222222222222`

**Requête :**
```json
{
  "equipmentId": "a1b2c3d4-5678-90ab-cdef-111111111111",
  "type": "Emergency",
  "description": "URGENT - Fuite d'eau importante au niveau du circulateur",
  "estimatedDurationMinutes": 120,
  "notes": "Client signale une flaque d'eau importante - priorité haute"
}
```

**Réponse (200 OK) :** Intervention mise à jour

---

#### DELETE /api/interventions/{id}

Supprime une intervention (impossible si en cours ou terminée).

**Réponse (204 No Content)**

**Erreurs possibles :**
| Code | Description |
|------|-------------|
| 400 | Impossible de supprimer une intervention en cours ou terminée |
| 404 | Intervention non trouvée |

---

#### POST /api/interventions/{id}/assign

Affecte un technicien à une intervention et planifie la date.

**Exemple :** `POST /api/interventions/22222222-2222-2222-2222-222222222222/assign`

**Requête :**
```json
{
  "technicianId": "tech-1111-2222-3333-444444444444",
  "scheduledDate": "2026-02-04",
  "scheduledStartTime": "14:00:00",
  "scheduledEndTime": "16:00:00"
}
```

**Réponse (200 OK) :**
```json
{
  "id": "22222222-2222-2222-2222-222222222222",
  "technicianId": "tech-1111-2222-3333-444444444444",
  "technicianName": "Pierre Leroy",
  "status": "Scheduled",
  "scheduledDate": "2026-02-04",
  "scheduledStartTime": "14:00:00",
  "scheduledEndTime": "16:00:00",
  "...": "autres champs"
}
```

---

#### POST /api/interventions/{id}/start

Démarre une intervention (réservé au technicien assigné).

**Exemple :** `POST /api/interventions/22222222-2222-2222-2222-222222222222/start`

**Réponse (200 OK) :**
```json
{
  "id": "22222222-2222-2222-2222-222222222222",
  "status": "InProgress",
  "startedAt": "2026-02-04T14:05:00Z",
  "...": "autres champs"
}
```

**Erreurs possibles :**
| Code | Description |
|------|-------------|
| 400 | L'intervention n'est pas dans un état permettant le démarrage |
| 403 | Seul le technicien assigné peut démarrer l'intervention |

---

#### POST /api/interventions/{id}/complete

Termine une intervention avec un rapport détaillé.

**Exemple :** `POST /api/interventions/22222222-2222-2222-2222-222222222222/complete`

**Requête :**
```json
{
  "technicianNotes": "Remplacement du joint torique du circulateur. Purge du circuit effectuée.",
  "report": {
    "checklist": [
      { "item": "Vérification pression circuit", "checked": true, "notes": "1.5 bar - OK" },
      { "item": "Contrôle étanchéité", "checked": true, "notes": "Plus de fuite constatée" },
      { "item": "Test fonctionnement", "checked": true, "notes": "Chaudière opérationnelle" }
    ],
    "photos": [
      {
        "url": "https://storage.exemple.com/photos/intervention-22222222-1.jpg",
        "description": "Circulateur après remplacement joint",
        "takenAt": "2026-02-04T15:30:00Z"
      }
    ],
    "clientSignature": {
      "signatureData": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...",
      "signedAt": "2026-02-04T15:45:00Z",
      "signerName": "Jean Dupont"
    },
    "partsUsed": [
      {
        "partNumber": "VI-7828774",
        "description": "Joint torique circulateur Viessmann",
        "quantity": 1,
        "unitPrice": 12.50
      }
    ],
    "measurements": {
      "pression_circuit": "1.5 bar",
      "temperature_depart": "55°C",
      "temperature_retour": "45°C"
    },
    "technicianComments": "Intervention terminée avec succès. Joint d'origine usé par le temps.",
    "recommendations": "Prévoir entretien annuel dans 10 mois. Surveiller la pression du circuit."
  }
}
```

**Réponse (200 OK) :**
```json
{
  "id": "22222222-2222-2222-2222-222222222222",
  "status": "Completed",
  "completedAt": "2026-02-04T15:50:00Z",
  "technicianNotes": "Remplacement du joint torique du circulateur. Purge du circuit effectuée.",
  "report": { "...": "rapport complet" },
  "...": "autres champs"
}
```

---

#### POST /api/interventions/{id}/cancel

Annule une intervention.

**Exemple :** `POST /api/interventions/22222222-2222-2222-2222-222222222222/cancel`

**Requête (optionnel) :**
```json
{
  "reason": "Client indisponible - report demandé"
}
```

**Réponse (200 OK) :**
```json
{
  "id": "22222222-2222-2222-2222-222222222222",
  "status": "Cancelled",
  "notes": "Client indisponible - report demandé",
  "...": "autres champs"
}
```

---

#### GET /api/interventions/my-interventions

Récupère les interventions du technicien connecté.

**Paramètres de requête :**
| Paramètre | Type | Description |
|-----------|------|-------------|
| date | DateTime (optionnel) | Filtrer par date |
| status | string (optionnel) | Filtrer par statut |

**Exemple :** `GET /api/interventions/my-interventions?date=2026-02-04`

**Réponse (200 OK) :** Liste des interventions du technicien

---

#### GET /api/interventions/planning

Récupère le planning des interventions sur une période.

**Paramètres de requête :**
| Paramètre | Type | Obligatoire | Description |
|-----------|------|-------------|-------------|
| startDate | DateTime | Oui | Date de début |
| endDate | DateTime | Oui | Date de fin |
| technicianId | GUID | Non | Filtrer par technicien |

**Exemple :** `GET /api/interventions/planning?startDate=2026-02-03&endDate=2026-02-09`

**Réponse (200 OK) :**
```json
{
  "startDate": "2026-02-03",
  "endDate": "2026-02-09",
  "interventions": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "scheduledDate": "2026-02-05",
      "scheduledStartTime": "09:00:00",
      "scheduledEndTime": "10:30:00",
      "clientName": "Famille Dupont",
      "technicianName": "Pierre Leroy",
      "type": "Maintenance",
      "status": "Scheduled"
    }
  ],
  "totalCount": 1
}
```

---

#### GET /api/interventions/statuses

Récupère la liste des statuts d'intervention.

**Réponse (200 OK) :**
```json
[
  { "value": 0, "name": "Pending" },
  { "value": 1, "name": "Scheduled" },
  { "value": 2, "name": "InProgress" },
  { "value": 3, "name": "Completed" },
  { "value": 4, "name": "Cancelled" }
]
```

---

#### GET /api/interventions/types

Récupère la liste des types d'intervention.

**Réponse (200 OK) :**
```json
[
  { "value": 0, "name": "Maintenance" },
  { "value": 1, "name": "Repair" },
  { "value": 2, "name": "Installation" },
  { "value": 3, "name": "Inspection" },
  { "value": 4, "name": "Emergency" }
]
```

---

### Géolocalisation API

#### POST /api/location/update

Met à jour la position GPS du technicien connecté.

**Requête :**
```json
{
  "latitude": 48.8566,
  "longitude": 2.3522,
  "accuracy": 10.5,
  "speed": 8.3,
  "heading": 45.0
}
```

**Réponse (200 OK) :**
```json
{
  "message": "Position mise à jour avec succès"
}
```

---

#### GET /api/location/technician/{technicianId}

Récupère la position actuelle d'un technicien.

**Exemple :** `GET /api/location/technician/tech-1111-2222-3333-444444444444`

**Réponse (200 OK) :**
```json
{
  "technicianId": "tech-1111-2222-3333-444444444444",
  "technicianName": "Pierre Leroy",
  "latitude": 48.8566,
  "longitude": 2.3522,
  "accuracy": 10.5,
  "speed": 8.3,
  "heading": 45.0,
  "timestamp": "2026-02-03T10:15:00Z",
  "isOnline": true
}
```

**Erreurs possibles :**
| Code | Description |
|------|-------------|
| 404 | Position non trouvée pour ce technicien |

---

#### GET /api/location/technicians

Récupère la position de tous les techniciens de l'organisation.

**Réponse (200 OK) :**
```json
[
  {
    "technicianId": "tech-1111-2222-3333-444444444444",
    "technicianName": "Pierre Leroy",
    "latitude": 48.8566,
    "longitude": 2.3522,
    "accuracy": 10.5,
    "speed": 8.3,
    "heading": 45.0,
    "timestamp": "2026-02-03T10:15:00Z",
    "isOnline": true
  },
  {
    "technicianId": "tech-5555-6666-7777-888888888888",
    "technicianName": "Sophie Bernard",
    "latitude": 48.8738,
    "longitude": 2.2950,
    "accuracy": 15.0,
    "speed": 0.0,
    "heading": null,
    "timestamp": "2026-02-03T10:12:00Z",
    "isOnline": true
  }
]
```

---

#### GET /api/location/geocode

Convertit une adresse en coordonnées GPS.

**Paramètres de requête :**
| Paramètre | Type | Obligatoire | Description |
|-----------|------|-------------|-------------|
| address | string | Oui | Adresse à géocoder |

**Exemple :** `GET /api/location/geocode?address=15%20Rue%20de%20la%20Paix%2C%2075002%20Paris`

**Réponse (200 OK) :**
```json
{
  "latitude": 48.8698,
  "longitude": 2.3311,
  "formattedAddress": "15 Rue de la Paix, 75002 Paris, France",
  "success": true,
  "errorMessage": null
}
```

**Réponse en cas d'erreur :**
```json
{
  "latitude": 0,
  "longitude": 0,
  "formattedAddress": null,
  "success": false,
  "errorMessage": "Adresse non trouvée"
}
```

---

#### GET /api/location/reverse-geocode

Convertit des coordonnées GPS en adresse.

**Paramètres de requête :**
| Paramètre | Type | Obligatoire | Description |
|-----------|------|-------------|-------------|
| latitude | double | Oui | Latitude |
| longitude | double | Oui | Longitude |

**Exemple :** `GET /api/location/reverse-geocode?latitude=48.8698&longitude=2.3311`

**Réponse (200 OK) :**
```json
{
  "address": "15 Rue de la Paix, 75002 Paris, France"
}
```

---

#### POST /api/location/optimize-route

Optimise un itinéraire passant par plusieurs points.

**Requête :**
```json
{
  "origin": {
    "latitude": 48.8566,
    "longitude": 2.3522,
    "label": "Départ - Bureau"
  },
  "waypoints": [
    {
      "latitude": 48.8698,
      "longitude": 2.3311,
      "label": "Client Dupont"
    },
    {
      "latitude": 48.8738,
      "longitude": 2.2950,
      "label": "Restaurant Le Gourmet"
    },
    {
      "latitude": 48.8520,
      "longitude": 2.3490,
      "label": "Boulangerie Au Pain Doré"
    }
  ],
  "destination": {
    "latitude": 48.8566,
    "longitude": 2.3522,
    "label": "Retour - Bureau"
  }
}
```

**Réponse (200 OK) :**
```json
{
  "optimizedRoute": [
    {
      "order": 0,
      "location": {
        "latitude": 48.8566,
        "longitude": 2.3522,
        "label": "Départ - Bureau"
      },
      "label": "Départ - Bureau",
      "distanceFromPreviousKm": 0,
      "durationFromPreviousMinutes": 0
    },
    {
      "order": 1,
      "location": {
        "latitude": 48.8520,
        "longitude": 2.3490,
        "label": "Boulangerie Au Pain Doré"
      },
      "label": "Boulangerie Au Pain Doré",
      "distanceFromPreviousKm": 0.8,
      "durationFromPreviousMinutes": 5
    },
    {
      "order": 2,
      "location": {
        "latitude": 48.8698,
        "longitude": 2.3311,
        "label": "Client Dupont"
      },
      "label": "Client Dupont",
      "distanceFromPreviousKm": 2.1,
      "durationFromPreviousMinutes": 12
    },
    {
      "order": 3,
      "location": {
        "latitude": 48.8738,
        "longitude": 2.2950,
        "label": "Restaurant Le Gourmet"
      },
      "label": "Restaurant Le Gourmet",
      "distanceFromPreviousKm": 2.8,
      "durationFromPreviousMinutes": 15
    },
    {
      "order": 4,
      "location": {
        "latitude": 48.8566,
        "longitude": 2.3522,
        "label": "Retour - Bureau"
      },
      "label": "Retour - Bureau",
      "distanceFromPreviousKm": 3.5,
      "durationFromPreviousMinutes": 18
    }
  ],
  "totalDistanceKm": 9.2,
  "totalDurationMinutes": 50,
  "success": true,
  "errorMessage": null
}
```

---

#### GET /api/location/distance

Calcule la distance et la durée entre deux points.

**Paramètres de requête :**
| Paramètre | Type | Obligatoire | Description |
|-----------|------|-------------|-------------|
| originLat | double | Oui | Latitude d'origine |
| originLng | double | Oui | Longitude d'origine |
| destLat | double | Oui | Latitude de destination |
| destLng | double | Oui | Longitude de destination |

**Exemple :** `GET /api/location/distance?originLat=48.8566&originLng=2.3522&destLat=48.8698&destLng=2.3311`

**Réponse (200 OK) :**
```json
{
  "distanceKm": 1.8,
  "durationMinutes": 8
}
```

---

## Codes d'erreur

| Code HTTP | Signification | Description |
|-----------|---------------|-------------|
| 200 | OK | Requête réussie |
| 201 | Created | Ressource créée avec succès |
| 204 | No Content | Suppression réussie |
| 400 | Bad Request | Données invalides ou requête mal formée |
| 401 | Unauthorized | Token manquant ou invalide |
| 403 | Forbidden | Accès refusé (permissions insuffisantes) |
| 404 | Not Found | Ressource non trouvée |
| 500 | Internal Server Error | Erreur serveur |

**Format des erreurs :**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Le champ 'name' est obligatoire.",
  "traceId": "00-1234567890abcdef-abcdef1234567890-00"
}
```

---

## Rôles et permissions

| Rôle | Description | Permissions |
|------|-------------|-------------|
| **Admin** | Administrateur de l'organisation | Accès complet à toutes les fonctionnalités |
| **Planificateur** | Responsable planification | Gestion clients, équipements, interventions, visualisation techniciens |
| **Technicien** | Technicien terrain | Voir ses interventions, mettre à jour sa position, démarrer/compléter interventions |

---

## Notes techniques

- **Multi-tenant** : Chaque organisation a ses propres données isolées
- **Géocodage automatique** : Les adresses des clients sont automatiquement converties en coordonnées GPS
- **Mise à jour maintenance** : Quand une intervention de type Maintenance est terminée, la date de dernière maintenance de l'équipement est automatiquement mise à jour
- **Temps réel** : La position des techniciens peut être mise à jour en temps réel via SignalR (WebSocket)
