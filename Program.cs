using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using GestionInterventionApi.Data;
using GestionInterventionApi.Services;
using GestionInterventionApi.Middleware;
using GestionInterventionApi.Hubs;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configuration SignalR.
//
// TenantHubFilter renseigne l'organisation courante avant chaque invocation :
// sans lui, le scope de dependances d'une methode de hub ignore le tenant
// (voir Hubs/TenantHubFilter.cs).
builder.Services.AddSignalR(options =>
{
    options.AddFilter<TenantHubFilter>();
});
builder.Services.AddSingleton<TenantHubFilter>();

// ---------------------------------------------------------------------------
// Validation de la configuration au demarrage.
//
// Avant : `builder.Configuration["Jwt:Key"]!` — l'operateur null-forgiving
// promettait au compilateur une valeur presente. Avec une variable absente,
// l'application demarrait quand meme et signait ses jetons avec une chaine
// vide : n'importe qui pouvait forger un jeton valide, sans le moindre message
// d'erreur. Un echec au demarrage est preferable a une faille silencieuse.
//
// Les secrets ne vivent plus dans appsettings.json (depot public) mais dans
// l'environnement : Jwt__Key, ConnectionStrings__DefaultConnection.
// ---------------------------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key absente ou trop courte : HMAC-SHA256 exige au moins 32 octets. " +
        "Definir la variable d'environnement Jwt__Key (voir appsettings.example.json). " +
        "Generer une valeur avec : openssl rand -base64 48");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection absente. Definir la variable "
        + "d'environnement ConnectionStrings__DefaultConnection "
        + "(voir appsettings.example.json).");
}

// Configuration Entity Framework Core avec PostgreSQL.
//
// Migre de SQL Server vers Npgsql : SQL Server sur Linux exige 2 Go de RAM a lui
// seul, plus que les six autres projets du portfolio reunis. PostgreSQL rejoint
// l'instance partagee du VPS. Aucune donnee n'a ete perdue au passage : la base
// Azure SQL d'origine n'existe plus.
//
// La chaine vient de l'environnement en production (ConnectionStrings__DefaultConnection).
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configuration JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey))
    };

    // Configuration pour SignalR - permet le token via query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// ---------------------------------------------------------------------------
// Autorisation par role (B-4).
//
// Avant : aucune policy, aucun [Authorize(Roles=...)]. Le seul controle etait
// inline dans start/complete. Un compte Technicien pouvait donc creer et
// desactiver des comptes, supprimer clients, equipements et interventions.
//
// Le nom du role vient de la revendication ClaimTypes.Role posee par
// AuthService.GenerateJwtToken, ou il vaut le nom de l'enum UserRole.
// ---------------------------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    // Ecriture sur les donnees metier : clients, equipements, interventions.
    options.AddPolicy("Gestion", policy =>
        policy.RequireRole(
            nameof(GestionInterventionApi.Models.Enums.UserRole.Admin),
            nameof(GestionInterventionApi.Models.Enums.UserRole.Planificateur)));

    // Cycle de vie des comptes : reserve a l'Admin. Un Planificateur organise le
    // travail, il n'administre pas les acces.
    options.AddPolicy("AdministrationComptes", policy =>
        policy.RequireRole(nameof(GestionInterventionApi.Models.Enums.UserRole.Admin)));
});

// Services Multi-tenant
builder.Services.AddScoped<ITenantService, TenantService>();

// Services Application
builder.Services.AddScoped<IAuthService, AuthService>();

// Demonstration publique : bacs a sable jetables et leur purge.
builder.Services.AddScoped<IDemoService, DemoService>();
builder.Services.AddHostedService<DemoCleanupService>();

// L'API ne publie aucun port : en production, seul Caddy peut l'atteindre. Sans
// ce middleware, RemoteIpAddress vaut l'adresse de la passerelle Docker, la meme
// pour tout le monde — et la limitation « par visiteur » ci-dessous deviendrait
// une limitation globale, sans que rien ne le signale.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Le reseau Docker attribue les adresses dynamiquement : on ne peut pas
    // enumerer le mandataire. C'est acceptable ici, et seulement ici, parce que
    // le conteneur est inatteignable autrement que par Caddy.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    // Remonter TOUTE la chaine, pas seulement le dernier relais.
    //
    // Mesure du 31 aout 2026. En production la chaine est :
    //   visiteur -> edge Vercel -> Caddy -> ce conteneur
    // Vercel transmet correctement l'adresse du visiteur, puis Caddy ajoute
    // celle de l'edge Vercel qu'il a vu. Le conteneur recoit donc
    // « X-Forwarded-For: <visiteur>, <edge Vercel> ».
    //
    // ForwardLimit vaut 1 par defaut : ASP.NET ne depile que l'element de
    // DROITE, donc l'adresse de l'edge Vercel — et celle-ci ALTERNE d'une
    // requete a l'autre (35.182.251.83 / 15.156.206.244 observees). La
    // limitation « par visiteur » changeait ainsi de compteur a chaque appel :
    // cinq creations de bac a sable d'affilee passaient toutes, alors que la
    // limite est de trois. Le defaut ne se voyait qu'a travers Vercel, jamais
    // en appelant l'API directement.
    //
    // Avec null, la chaine est depilee entierement et RemoteIpAddress designe
    // le visiteur, par les deux chemins.
    //
    // Limite assumee : sur le domaine de l'API, joignable sans passer par
    // Vercel, un appelant peut forger cet en-tete et se donner une adresse par
    // requete. Le garde-fou qui tient alors est le plafond de bacs a sable
    // vivants, qui ne depend d'aucun en-tete.
    options.ForwardLimit = null;
});

// Creation de bacs a sable : par defaut 3 par tranche de 10 minutes et par
// adresse IP. Assez pour qu'un visiteur recommence s'il se trompe, trop peu
// pour qu'un robot remplisse la base. Reglable sans recompiler
// (Demo__RateLimitPermits, Demo__RateLimitWindowMinutes).
var demoPermis = builder.Configuration.GetValue("Demo:RateLimitPermits", 3);
var demoFenetre = builder.Configuration.GetValue("Demo:RateLimitWindowMinutes", 10);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("demo", contexte => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: contexte.Connection.RemoteIpAddress?.ToString() ?? "inconnu",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = demoPermis,
            Window = TimeSpan.FromMinutes(demoFenetre),
            QueueLimit = 0
        }));
});

// Services Géolocalisation
builder.Services.AddScoped<ILocationTrackingService, LocationTrackingService>();
// B-10 — delai d'attente explicite sur les appels sortants vers Google.
//
// Le defaut de HttpClient est de 100 secondes. Une API Google lente ou
// injoignable immobilisait donc une requete utilisateur pendant plus d'une
// minute et demie, et autant de threads que d'appels simultanes. 10 secondes
// suffisent largement pour du geocodage ; au-dela, le repli (distance a vol
// d'oiseau) vaut mieux qu'une attente.
builder.Services.AddHttpClient<IGeocodingService, GeocodingService>(
    c => c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddHttpClient<IRouteOptimizationService, RouteOptimizationService>(
    c => c.Timeout = TimeSpan.FromSeconds(10));

// AutoMapper 16.
//
// La version 12.0.1 portait un avis de securite de gravite elevee
// (GHSA-rvv3-g6hj-g44x) et son paquet compagnon
// AutoMapper.Extensions.Microsoft.DependencyInjection est abandonne : depuis la
// version 13, l'enregistrement DI vit dans le paquet principal. La signature a
// change au passage — la configuration se declare maintenant explicitement.
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ---------------------------------------------------------------------------
// CORS (B-14).
//
// Avant : `SetIsOriginAllowed(_ => true)` combine a `AllowCredentials()`.
// ASP.NET Core interdit `AllowAnyOrigin()` avec des credentials ; ce predicat
// contournait l'interdiction en RENVOYANT l'en-tete Origin de l'appelant, quel
// qu'il soit, avec `Access-Control-Allow-Credentials: true`. N'importe quel site
// pouvait donc emettre des requetes authentifiees et negocier SignalR. Le
// commentaire « en production, specifier les origines » n'a jamais ete suivi.
//
// Desormais : liste blanche lue dans la configuration (Cors:AllowedOrigins,
// separees par des virgules), avec repli sur le front local en developpement.
// Aucune valeur par defaut permissive.
var originesAutorisees = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:4200")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.WithOrigins(originesAutorisees)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// B-8 — gestion globale des exceptions (voir Middleware/GestionnaireExceptions.cs).
builder.Services.AddExceptionHandler<GestionnaireExceptions>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Migrations au demarrage, sur demande explicite.
//
// L'image ne connait pas `dotnet ef` (l'outil vit dans le SDK, pas dans le
// runtime) : sans ce bloc, chaque deploiement exigerait un geste manuel depuis
// un poste ayant acces a la base — c'est-a-dire, en pratique, un schema qui
// derive un jour ou l'autre.
//
// Sous condition, jamais par defaut : appliquer des migrations est une ecriture
// de schema, elle doit etre voulue. RUN_MIGRATIONS_ON_BOOT=true dans la pile de
// deploiement ; absente en developpement, ou l'on veut garder la main.
if (string.Equals(builder.Configuration["RUN_MIGRATIONS_ON_BOOT"], "true",
                  StringComparison.OrdinalIgnoreCase))
{
    using var portee = app.Services.CreateScope();
    var contexte = portee.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var journal = portee.ServiceProvider.GetRequiredService<ILogger<Program>>();

    journal.LogInformation("Application des migrations en attente…");
    await contexte.Database.MigrateAsync();
    journal.LogInformation("Schema a jour.");
}

// Doit preceder tout ce qui lit l'adresse du client (journalisation comprise),
// sinon chacun voit l'adresse de la passerelle Docker au lieu du visiteur.
app.UseForwardedHeaders();

// Le gestionnaire d'exceptions doit etre le PREMIER middleware du pipeline :
// il n'attrape que ce qui remonte des middlewares places apres lui.
app.UseExceptionHandler();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
   
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("SignalRPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Middleware Multi-tenant (après l'authentification)
app.UseTenantMiddleware();

app.MapControllers();

// Map SignalR Hub
app.MapHub<LocationHub>("/hubs/location");

app.Run();
