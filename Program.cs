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

// Configuration Entity Framework Core avec SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
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
