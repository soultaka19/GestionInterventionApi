# CLAUDE.md — GestionInterventionApi

> B2B micro-SaaS for field intervention management (GestionIntervention).
> Backend: ASP.NET Core 10 Web API. Frontend: Angular 21 at `C:\workflow\gestion-intervention`.

---

## Stack & Versions

| Layer | Technology | Version |
|-------|-----------|---------|
| Runtime | .NET | 10.0 |
| Framework | ASP.NET Core Web API | 10.0 |
| ORM | Entity Framework Core (SQL Server) | 10.0.2 |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) | 10.0.2 |
| Mapping | AutoMapper | 12.0.1 |
| Validation | FluentValidation.AspNetCore | 11.3.1 |
| Logging | Serilog.AspNetCore | 10.0.0 |
| Real-time | SignalR (built-in ASP.NET Core) | 10.0 |
| API Docs | Scalar.AspNetCore (dev only) | 2.x |
| Frontend | Angular + PrimeNG + Tailwind CSS | 21 |
| Maps | Leaflet | 1.9 |
| Charts | Chart.js | 4.x |

---

## Project Structure

```
GestionInterventionApi/
├── Controllers/          # API controllers (one per domain)
│   ├── AuthController.cs
│   ├── ClientsController.cs
│   ├── EquipmentsController.cs
│   ├── InterventionsController.cs
│   ├── LocationController.cs
│   └── TechniciansController.cs
├── Data/
│   └── ApplicationDbContext.cs   # EF Core DbContext, multi-tenant query filters
├── DTOs/                 # Input/Output DTOs, organized by domain
│   ├── Auth/             # LoginDto, RegisterDto, AuthResponseDto
│   ├── Client/           # ClientDto, CreateClientDto, UpdateClientDto, ClientDetailDto
│   ├── Equipment/        # EquipmentDto, CreateEquipmentDto, UpdateEquipmentDto
│   └── Intervention/     # InterventionDto, CreateInterventionDto, AssignTechnicianDto, CompleteInterventionDto, InterventionDetailDto
├── Hubs/
│   └── LocationHub.cs    # SignalR hub — real-time technician location
├── Mappings/
│   └── MappingProfile.cs # AutoMapper profile
├── Middleware/
│   └── TenantMiddleware.cs  # Extracts OrganizationId from JWT, sets ITenantService
├── Migrations/           # EF Core code-first migrations
├── Models/               # Domain entities
│   ├── BaseEntity.cs     # Id (Guid), CreatedAt, UpdatedAt
│   ├── Organization.cs   # Tenant root
│   ├── User.cs           # Linked to Organization, has UserRole
│   ├── Client.cs
│   ├── Equipment.cs
│   ├── Intervention.cs   # Core entity
│   ├── InterventionReport.cs
│   ├── TechnicianLocation.cs
│   ├── ITenantEntity.cs  # Interface: OrganizationId property
│   └── Enums/
│       ├── UserRole.cs           # Admin, Planificateur, Technicien
│       ├── InterventionStatus.cs # Pending, Scheduled, InProgress, Completed, Cancelled
│       ├── InterventionType.cs
│       ├── EquipmentType.cs
│       └── SubscriptionPlan.cs
├── Services/             # Business logic, always injected via interface
│   ├── IAuthService / AuthService.cs
│   ├── ITenantService / TenantService.cs
│   ├── ILocationTrackingService / LocationTrackingService.cs
│   ├── IGeocodingService / GeocodingService.cs      # HttpClient — Google Maps
│   └── IRouteOptimizationService / RouteOptimizationService.cs  # HttpClient
├── Validators/           # FluentValidation, organized by domain
│   ├── Auth/
│   ├── Client/
│   ├── Equipment/
│   ├── Intervention/
│   ├── Organization/
│   └── User/
├── Program.cs            # App entry point, all DI registrations
├── appsettings.json      # Config (DB, JWT, Google Maps, Serilog)
└── appsettings.Development.json
```

### Frontend (Angular 21) — `C:\workflow\gestion-intervention`

```
src/app/
├── core/
│   ├── auth/             # Guards, interceptors
│   └── services/         # Shared Angular services
├── features/             # Feature modules (lazy-loaded)
│   ├── auth/
│   ├── client/
│   ├── equipment/
│   ├── geolocation/
│   ├── intervention/
│   ├── planning/
│   └── technician/
├── layout/
│   ├── navbar/
│   └── sidebar/
└── dashbord/             # Dashboard (note: folder name typo retained)
```

---

## Architecture Patterns

### Multi-tenancy
- Every domain entity implements `ITenantEntity` (has `OrganizationId: Guid`).
- `TenantMiddleware` reads `OrganizationId` from the authenticated JWT claim and populates `ITenantService`.
- `ApplicationDbContext` applies a global EF Core query filter on all `ITenantEntity` types so queries are automatically scoped to the current tenant.
- On `SaveChanges`, the context automatically sets `OrganizationId` on new entities if not already set.

### Authentication & Authorization
- JWT Bearer tokens. Config in `appsettings.json` under `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`.
- Three roles: `Admin`, `Planificateur`, `Technicien` (stored as string in DB via EF value conversion).
- SignalR also accepts the JWT via query string `?access_token=...` for WebSocket connections.

### Data Access
- EF Core Code-First. All entity configs in `OnModelCreating` (no separate `IEntityTypeConfiguration` files currently).
- `BaseEntity`: all entities inherit it — provides `Id` (Guid), `CreatedAt`, `UpdatedAt` (auto-set by `SaveChanges`).
- Enums stored as strings in DB (`HasConversion<string>()`).

### Validation
- FluentValidation with auto-validation (`AddFluentValidationAutoValidation()`).
- One validator per DTO, placed in `Validators/<Domain>/`.

### Mapping
- AutoMapper, single profile in `Mappings/MappingProfile.cs`.
- Never map directly in controllers — use `IMapper`.

### Real-time
- SignalR hub: `LocationHub` at `/hubs/location`.
- Used for broadcasting technician GPS position after REST updates.

### Logging
- Serilog: Console + rolling daily File (`Logs/log-YYYYMMDD.txt`).
- `app.UseSerilogRequestLogging()` for HTTP request logs.

### API Documentation
- Scalar (replaces Swagger/Swashbuckle) — available at `/scalar/v1` in Development only.
- OpenAPI spec at `/openapi/v1.json`.

---

## Essential Commands

### Build & Run
```bash
dotnet build
dotnet run
dotnet watch run          # hot-reload dev
```

### Database Migrations (EF Core)
```bash
# Add a new migration
dotnet ef migrations add <MigrationName>

# Apply migrations to the database
dotnet ef database update

# Revert last migration (before applying)
dotnet ef migrations remove

# Generate SQL script (for production review)
dotnet ef migrations script
```

### Publish
```bash
dotnet publish -c Release -o ./publish
```

### Frontend (Angular)
```bash
cd C:\workflow\gestion-intervention
npm start          # ng serve — dev server
npm run build      # production build
npm test           # vitest
```

---

## Configuration

### appsettings.json (key sections)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TechMaintDb;..."
  },
  "Jwt": {
    "Key": "<min 32 chars secret>",
    "Issuer": "TechMaintApi",
    "Audience": "TechMaintClients",
    "ExpireMinutes": 60
  },
  "GoogleMaps": {
    "ApiKey": "<secret>",
    "GeocodingEnabled": true,
    "DirectionsEnabled": true
  },
  "Tracking": {
    "UpdateIntervalSeconds": 60,
    "OfflineThresholdMinutes": 5
  }
}
```

**Never commit real secrets.** Use `appsettings.Development.json` (git-ignored) or environment variables / Azure Key Vault for production.

---

## Domain Model Summary

| Entity | Key Relations |
|--------|--------------|
| `Organization` | Root tenant. Has many `User`. |
| `User` | Belongs to `Organization`. Role: Admin/Planificateur/Technicien. |
| `Client` | Tenant-scoped. Has many `Equipment`. |
| `Equipment` | Belongs to `Client` + `Organization`. |
| `Intervention` | Belongs to `Client`, optional `Equipment`, optional `Technician` (User), `Organization`. |
| `TechnicianLocation` | 1:1 with `User` (Technician). Stores last known GPS position. |

### InterventionStatus lifecycle
```
Pending → Scheduled → InProgress → Completed
                   ↘              ↘
                   Cancelled      Cancelled
```

---

## Code Conventions

- **Controllers**: `[ApiController]`, `[Route("api/[controller]")]`. Return `ActionResult<T>`. No business logic — delegate to services.
- **Services**: Always injected via interface (`IXxxService`). Scoped lifetime (`AddScoped`).
- **DTOs**: Separate Create/Update/Response DTOs per entity. Never expose domain models directly.
- **Validation**: `AbstractValidator<TDto>` in `Validators/<Domain>/`. Rules via `.RuleFor().NotEmpty().MaximumLength()...`
- **Namespace**: `GestionInterventionApi.<Folder>` (matches folder structure).
- **Async**: All service methods and controller actions are `async Task<T>`.
- **Nullable**: Enabled project-wide. Use `!` (null-forgiving) only when null is structurally impossible.

---

## CORS & SignalR

CORS policy `"SignalRPolicy"` is applied globally. It allows any origin with credentials (for SignalR WebSockets). **Restrict origins in production.**

---

## Deployment Notes

- Target: Azure App Service (see `azure-logs/`, `azure-logs2/` folders).
- Publish artifacts go to `./publish/` or `./publish-deploy/`.
- Deployment scripts: `mkzip.py`, `zip_deploy.py` (Zip Deploy to Azure).
- Branch: `master` is main. Feature branches merge via PR.
