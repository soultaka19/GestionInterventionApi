using System.Text;
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

// Configuration SignalR
builder.Services.AddSignalR();

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

builder.Services.AddAuthorization();

// Services Multi-tenant
builder.Services.AddScoped<ITenantService, TenantService>();

// Services Application
builder.Services.AddScoped<IAuthService, AuthService>();

// Services Géolocalisation
builder.Services.AddScoped<ILocationTrackingService, LocationTrackingService>();
builder.Services.AddHttpClient<IGeocodingService, GeocodingService>();
builder.Services.AddHttpClient<IRouteOptimizationService, RouteOptimizationService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// CORS pour SignalR (ajuster selon vos besoins)
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true); // En production, spécifier les origines autorisées
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
   
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("SignalRPolicy");

app.UseAuthentication();
app.UseAuthorization();

// Middleware Multi-tenant (après l'authentification)
app.UseTenantMiddleware();

app.MapControllers();

// Map SignalR Hub
app.MapHub<LocationHub>("/hubs/location");

app.Run();
