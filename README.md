# TechMaint — API

**API .NET de gestion d'interventions terrain pour plusieurs organisations sur une même instance, cloisonnées jusque dans les canaux temps réel.**

**Démonstration ouverte, sans inscription : [techmaint.soultaka.com/demo](https://techmaint.soultaka.com/demo)**

---

## Le problème

Une plateforme multi-locataire est facile à écrire et difficile à rendre étanche.

Le filtrage par organisation se met naturellement dans les contrôleurs HTTP, où il est visible et testé. Il s'oublie partout ailleurs : dans les tâches de fond, dans les rapports, et surtout dans les canaux temps réel, où le contexte de la requête HTTP n'existe plus.

Cette API est construite sur ce point précis.

## La solution

**L'isolation est posée au niveau du contexte de données, pas des contrôleurs.** Toute entité rattachée à une organisation implémente `ITenantEntity`, et le filtrage est appliqué globalement à la source. Un développeur qui ajoute une requête n'a pas à se souvenir d'ajouter la condition : il faudrait un effort délibéré pour la contourner.

**Le temps réel refuse l'abonnement plutôt que de filtrer la lecture.** Le suivi de position des techniciens passe par SignalR, où le contexte HTTP n'est pas transmis. Un `TenantHubFilter` résout l'organisation à la connexion : un client d'une autre organisation ne reçoit pas des messages vides, il ne s'abonne jamais.

**Le mode démonstration se nettoie tout seul.** Chaque visiteur de `/demo` obtient une organisation isolée, peuplée de données réalistes puis détruite par un service de nettoyage. C'est ce qui permet d'ouvrir la démonstration sans inscription, sans qu'un visiteur voie le travail d'un autre.

## Ce que couvre l'API

Organisations et comptes, clients, équipements, techniciens, interventions et rapports d'intervention, planification, géolocalisation des techniciens, optimisation d'itinéraires et agrégats de tableau de bord.

## Architecture

Le produit vit dans deux dépôts :

| | Dépôt | Rôle |
|---|---|---|
| **API** | ce dépôt | API REST, base de données, temps réel |
| **Front-end** | `gestion-intervention` | Application Angular |

```
Controllers/   points d'entrée REST
Hubs/          SignalR — suivi temps réel des techniciens
Services/      règles métier
Data/          DbContext, filtre global de locataire, migrations
Models/        entités, dont ITenantEntity
DTOs/          contrats d'entrée et de sortie
Validators/    FluentValidation
Middleware/    gestion d'erreurs, résolution du locataire
Mappings/      AutoMapper
```

## Décisions techniques

**SQL Server a été abandonné pour PostgreSQL en cours de projet.** SQL Server sur Linux exige 2 Go de RAM au minimum — davantage que tous les autres services de la machine réunis. Le portage EF Core vers Npgsql a coûté moins cher que le serveur qu'il aurait fallu louer.

**Les migrations sont appliquées au démarrage, sous condition explicite.** `dotnet ef` vit dans le SDK, pas dans l'image `aspnet` de production : sans cet appel, chaque déploiement demanderait un geste manuel depuis un poste ayant accès à la base, et le schéma dériverait.

**`ASPNETCORE_URLS` est fixé dans l'image.** `launchSettings.json` n'est pas publié mais gagne en développement, ce qui fait écouter un tout autre port que celui qu'on croit.

**La limitation de débit lit `X-Vercel-Forwarded-For` en premier.** Derrière un reverse proxy, `X-Forwarded-For` est remplacé par le proxy lui-même : l'API ne voyait que l'adresse de l'edge, qui change d'une requête à l'autre, et chaque appel tombait dans un compteur différent. L'en-tête propre au proxy traverse intact.

## Pile technique

`.NET` · `C#` · `ASP.NET Core` · `Entity Framework Core` · `PostgreSQL` (`Npgsql`) · `SignalR` · `JWT` · `AutoMapper` · `FluentValidation` · `Serilog` · `OpenAPI / Scalar` · `Docker`

## Lancer en local

```bash
cp appsettings.example.json appsettings.json   # renseigner la chaîne de connexion et la clé JWT
dotnet restore
dotnet run
```

La documentation interactive est servie sur `/scalar` une fois l'API démarrée.

`appsettings.json` est versionné avec ses valeurs sensibles vides : aucune clé réelle ne vit dans ce dépôt.

## Déploiement

Image Docker, derrière Caddy sur un VPS, avec PostgreSQL partagé. Les scripts sont dans `deploy/`.

Les appels REST du front passent par une réécriture edge, plus rapide qu'un appel direct et sans requête préalable CORS. **Le hub SignalR, lui, vise l'API en direct** : une réécriture HTTP ne relaie pas la montée en `Upgrade` d'un WebSocket, et un client SignalR y retomberait au mieux sur du long-polling.

## Documentation

**[Documentation-API.md](Documentation-API.md)** : les points d'entrée, leurs contrats et leurs codes de retour.

---

Souleymane Diallo · [soultaka.com](https://soultaka.com) · [linkedin.com/in/souleyman-dev](https://linkedin.com/in/souleyman-dev)
