# 🛠️ Plan de Développement Backend - SaaS Gestion Interventions

## 📌 Présentation du Projet
**Nom :** TechMaint (Provisoire)  
**Objectif :** Application SaaS de gestion d'interventions pour PME de chauffage et maintenance thermique.  
**Stack Technique :** .NET 10 Web API, Entity Framework Core, SQL Server.

---

## 🏗️ Architecture & Environnement de Travail

### ⚙️ Environnement de Développement
- **IDE :** Visual Studio 2026  
- **SDK :** .NET 10 SDK  
- **Base de données :** SQL Server (LocalDB ou Express)
- **Géo-localisation :** Services de Géocodage (Mapbox ou Google Maps) pour Lat/Long.

### 🛡️ Stratégie Multi-Tenant
- **Modèle :** Base de données partagée (**Shared Schema**).
- **Isolation :** Utilisation d'une colonne `OrganizationId` sur toutes les tables liées aux clients.
- **Sécurité :** Implémentation de `HasQueryFilter` dans EF Core pour injecter automatiquement le filtrage par organisation via le Token JWT.

### 📦 Dépendances Principales (NuGet)
| Bibliothèque | Version | Utilité |
| :--- | :--- | :--- |
| `Microsoft.EntityFrameworkCore.SqlServer`  | Accès à la base de données |
| `Microsoft.EntityFrameworkCore.Design` | Migrations de base de données |
| `Microsoft.AspNetCore.Authentication.JwtBearer`| Sécurisation JWT |
| `AutoMapper` | Mapping entre Entités et DTOs |
| `FluentValidation` || Validation des données d'entrée |
| `Serilog.AspNetCore` || Logging structuré |

---

## 🗄️ Modèle de Données (Priorité Backend)


### 1. Structure SaaS & Auth
- **Organization :** `Id`, `Name`, `SubscriptionPlan` (Free, Pro), `CreatedAt`.
- **User :** `Id`, `Email`, `PasswordHash`, `Role` (Admin, Planificateur, Technicien), `OrganizationId`.

### 2. Métier (Gestion de Chauffage)
- **Client :** `Id`, `Name`, `Address`, `Latitude`, `Longitude`, `OrganizationId`.
- **Equipment :** `Id`, `ClientId`, `Type` (Chaudière, Radiateur), `Brand`, `OrganizationId`.
- **Intervention :**
    - `Id`, `OrganizationId`, `ClientId`, `TechnicianId`, `EquipmentId`.
    - `ScheduledDate` (Date seule), `Status` (Pending, InProgress, Completed).
    - `ReportJson` : Contient la checklist fixe, les URLs des photos et les coordonnées de la signature 
fin du fichier