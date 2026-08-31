using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using GestionInterventionApi.Data;
using GestionInterventionApi.DTOs.Auth;
using GestionInterventionApi.DTOs.Demo;
using GestionInterventionApi.Models;
using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.Services;

/// <summary>
/// Fabrique des bacs a sable jetables pour les visiteurs.
///
/// Le principe tient en une phrase : un visiteur recoit sa propre organisation.
/// L'isolation n'est pas ajoutee pour la demonstration, c'est celle du produit —
/// le filtre de requete fail-closed d'ApplicationDbContext. Deux visiteurs ne
/// peuvent pas se voir, et aucun ne voit les organisations reelles.
/// </summary>
public class DemoService : IDemoService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;
    private readonly ILogger<DemoService> _logger;

    /// <summary>Duree de vie d'un bac a sable (Demo__LifetimeMinutes).</summary>
    private readonly TimeSpan _dureeDeVie;

    /// <summary>
    /// Plafond de bacs a sable vivants simultanement (Demo__MaxLiveSandboxes).
    /// Garde-fou de dernier recours : la limitation de debit par adresse IP ne
    /// protege pas d'un flux distribue sur plusieurs adresses.
    /// </summary>
    private readonly int _plafondBacsVivants;

    public DemoService(
        ApplicationDbContext context,
        IAuthService authService,
        IConfiguration configuration,
        ILogger<DemoService> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
        _dureeDeVie = TimeSpan.FromMinutes(configuration.GetValue("Demo:LifetimeMinutes", 60));
        _plafondBacsVivants = configuration.GetValue("Demo:MaxLiveSandboxes", 50);
    }

    public async Task<DemoSessionDto?> CreateSandboxAsync(CancellationToken cancellation = default)
    {
        var maintenant = DateTime.UtcNow;

        var vivants = await _context.Organizations
            .CountAsync(o => o.IsDemo && o.ExpiresAt > maintenant, cancellation);

        if (vivants >= _plafondBacsVivants)
        {
            _logger.LogWarning("Plafond de bacs a sable atteint ({Vivants})", vivants);
            return null;
        }

        // Suffixe court et non devinable : il rend les adresses courriel uniques
        // (l'index sur User.Email est unique a l'echelle de la base) et evite
        // qu'un visiteur devine l'adresse du bac a sable d'un autre.
        var suffixe = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
        var motDePasse = $"Demo-{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}";

        var organisation = new Organization
        {
            Id = Guid.NewGuid(),
            Name = $"Démo {suffixe}",
            SubscriptionPlan = SubscriptionPlan.Pro,
            IsDemo = true,
            ExpiresAt = maintenant.Add(_dureeDeVie)
        };

        var hash = AuthService.HashPassword(motDePasse);

        var admin = CreerUtilisateur(organisation.Id, $"admin@{suffixe}.demo.test", hash,
            "Sonia", "Bélanger", UserRole.Admin);
        var planificateur = CreerUtilisateur(organisation.Id, $"planif@{suffixe}.demo.test", hash,
            "Marc", "Tremblay", UserRole.Planificateur);
        var technicien = CreerUtilisateur(organisation.Id, $"tech@{suffixe}.demo.test", hash,
            "Yanis", "Ferland", UserRole.Technicien);
        var technicienne = CreerUtilisateur(organisation.Id, $"tech2@{suffixe}.demo.test", hash,
            "Claire", "Dubois", UserRole.Technicien);

        _context.Organizations.Add(organisation);
        _context.Users.AddRange(admin, planificateur, technicien, technicienne);

        AlimenterDonnees(organisation.Id, technicien.Id, technicienne.Id, maintenant);

        await _context.SaveChangesAsync(cancellation);

        // On passe par la connexion normale : le jeton du visiteur est produit
        // exactement comme celui d'un vrai compte, avec la revendication
        // OrganizationId dont dependent le filtre de requete et TenantHubFilter.
        var session = await _authService.LoginAsync(new LoginDto(admin.Email, motDePasse));
        if (session is null)
        {
            _logger.LogError("Bac a sable {Id} cree mais la connexion a echoue", organisation.Id);
            return null;
        }

        _logger.LogInformation(
            "Bac a sable {Id} cree, expire a {Expiration}", organisation.Id, organisation.ExpiresAt);

        return new DemoSessionDto(
            Token: session.Token,
            TokenExpiresAt: session.ExpiresAt,
            User: session.User,
            OrganizationId: organisation.Id,
            OrganizationName: organisation.Name,
            SandboxExpiresAt: organisation.ExpiresAt!.Value,
            SharedPassword: motDePasse,
            Accounts: new List<DemoAccountDto>
            {
                new(admin.Email, admin.Role, admin.FirstName, admin.LastName),
                new(planificateur.Email, planificateur.Role, planificateur.FirstName, planificateur.LastName),
                new(technicien.Email, technicien.Role, technicien.FirstName, technicien.LastName)
            });
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellation = default)
    {
        var maintenant = DateTime.UtcNow;

        var expires = await _context.Organizations
            .Where(o => o.IsDemo && o.ExpiresAt != null && o.ExpiresAt < maintenant)
            .Select(o => o.Id)
            .ToListAsync(cancellation);

        if (expires.Count == 0)
        {
            return 0;
        }

        // L'ordre compte : toutes les relations vers Organization sont en
        // DeleteBehavior.Restrict, donc l'organisation ne peut partir qu'en
        // dernier. IgnoreQueryFilters est indispensable — hors requete HTTP,
        // ITenantService ne porte aucune organisation et le filtre fail-closed
        // ne renverrait rien du tout.
        await _context.Interventions.IgnoreQueryFilters()
            .Where(e => expires.Contains(e.OrganizationId)).ExecuteDeleteAsync(cancellation);
        await _context.TechnicianLocations.IgnoreQueryFilters()
            .Where(e => expires.Contains(e.OrganizationId)).ExecuteDeleteAsync(cancellation);
        await _context.Equipments.IgnoreQueryFilters()
            .Where(e => expires.Contains(e.OrganizationId)).ExecuteDeleteAsync(cancellation);
        await _context.Clients.IgnoreQueryFilters()
            .Where(e => expires.Contains(e.OrganizationId)).ExecuteDeleteAsync(cancellation);
        await _context.Users.IgnoreQueryFilters()
            .Where(e => expires.Contains(e.OrganizationId)).ExecuteDeleteAsync(cancellation);
        await _context.Organizations
            .Where(o => expires.Contains(o.Id)).ExecuteDeleteAsync(cancellation);

        _logger.LogInformation("{Nombre} bac(s) a sable expire(s) supprime(s)", expires.Count);
        return expires.Count;
    }

    private static User CreerUtilisateur(
        Guid organisationId, string courriel, string hash,
        string prenom, string nom, UserRole role) => new()
        {
            Id = Guid.NewGuid(),
            Email = courriel,
            PasswordHash = hash,
            FirstName = prenom,
            LastName = nom,
            Role = role,
            IsActive = true,
            OrganizationId = organisationId
        };

    /// <summary>
    /// Jeu de donnees fictif, localise a Ottawa-Gatineau pour que la carte soit
    /// peuplee des la premiere seconde, et etale dans le temps pour que le
    /// planning et les statuts ne soient pas tous identiques.
    /// </summary>
    private void AlimenterDonnees(
        Guid organisationId, Guid technicienId, Guid technicienneId, DateTime maintenant)
    {
        var aujourdhui = maintenant.Date;

        var clients = new[]
        {
            CreerClient(organisationId, "Tour Rideau", "150 rue Rideau", "Ottawa", "K1N 5X6",
                45.4275, -75.6903, "gestion@tour-rideau.demo.test", "613-555-0142"),
            CreerClient(organisationId, "Résidence Kanata Nord", "700 promenade March", "Kanata", "K2K 2E1",
                45.3088, -75.8988, "entretien@kanata-nord.demo.test", "613-555-0177"),
            CreerClient(organisationId, "Centre commercial Orléans", "110 boulevard Place d'Orléans", "Orléans", "K1C 2L9",
                45.4657, -75.5183, "technique@orleans-centre.demo.test", "613-555-0198"),
            CreerClient(organisationId, "Complexe Laurier", "25 rue Laurier", "Gatineau", "J8X 4C8",
                45.4290, -75.7100, "batiment@complexe-laurier.demo.test", "819-555-0163")
        };

        var equipements = new[]
        {
            CreerEquipement(organisationId, clients[0].Id, EquipmentType.Chaudiere, "Viessmann", "Vitodens 200",
                aujourdhui.AddYears(-6), aujourdhui.AddMonths(-8)),
            CreerEquipement(organisationId, clients[0].Id, EquipmentType.Ventilation, "Venmar", "AVS series",
                aujourdhui.AddYears(-3), aujourdhui.AddMonths(-2)),
            CreerEquipement(organisationId, clients[1].Id, EquipmentType.PompeAChaleur, "Mitsubishi", "Zuba Central",
                aujourdhui.AddYears(-2), aujourdhui.AddMonths(-5)),
            CreerEquipement(organisationId, clients[2].Id, EquipmentType.Climatisation, "Carrier", "Infinity 26",
                aujourdhui.AddYears(-4), aujourdhui.AddMonths(-14)),
            CreerEquipement(organisationId, clients[2].Id, EquipmentType.ChauffeEau, "Rheem", "Professional",
                aujourdhui.AddYears(-9), aujourdhui.AddMonths(-11)),
            CreerEquipement(organisationId, clients[3].Id, EquipmentType.Radiateur, "Runtal", "R-20",
                aujourdhui.AddYears(-12), null)
        };

        var interventions = new[]
        {
            // Terminees — le passe, pour que l'historique ne soit pas vide.
            CreerIntervention(organisationId, clients[0].Id, equipements[0].Id, technicienId,
                InterventionType.Maintenance, InterventionStatus.Completed,
                "Entretien annuel de la chaudière", aujourdhui.AddDays(-12), new TimeSpan(9, 0, 0), 90,
                demarre: aujourdhui.AddDays(-12).AddHours(9), termine: aujourdhui.AddDays(-12).AddHours(10).AddMinutes(20)),
            CreerIntervention(organisationId, clients[2].Id, equipements[3].Id, technicienneId,
                InterventionType.Repair, InterventionStatus.Completed,
                "Fuite de fluide frigorigène sur l'unité 3", aujourdhui.AddDays(-5), new TimeSpan(13, 30, 0), 120,
                demarre: aujourdhui.AddDays(-5).AddHours(13.5), termine: aujourdhui.AddDays(-5).AddHours(15).AddMinutes(45)),
            CreerIntervention(organisationId, clients[1].Id, equipements[2].Id, technicienId,
                InterventionType.Inspection, InterventionStatus.Completed,
                "Inspection de mise en service", aujourdhui.AddDays(-3), new TimeSpan(8, 0, 0), 60,
                demarre: aujourdhui.AddDays(-3).AddHours(8), termine: aujourdhui.AddDays(-3).AddHours(9)),

            // En cours — pour que le suivi en direct ait quelque chose a montrer.
            CreerIntervention(organisationId, clients[3].Id, equipements[5].Id, technicienneId,
                InterventionType.Repair, InterventionStatus.InProgress,
                "Radiateur froid au 4e étage", aujourdhui, new TimeSpan(10, 0, 0), 120,
                demarre: maintenant.AddMinutes(-35), termine: null),

            // Planifiees — aujourd'hui et les jours suivants.
            CreerIntervention(organisationId, clients[0].Id, equipements[1].Id, technicienId,
                InterventionType.Maintenance, InterventionStatus.Scheduled,
                "Remplacement des filtres de ventilation", aujourdhui, new TimeSpan(14, 0, 0), 60),
            CreerIntervention(organisationId, clients[2].Id, equipements[4].Id, technicienneId,
                InterventionType.Maintenance, InterventionStatus.Scheduled,
                "Détartrage du chauffe-eau", aujourdhui.AddDays(1), new TimeSpan(9, 30, 0), 90),
            CreerIntervention(organisationId, clients[1].Id, equipements[2].Id, technicienId,
                InterventionType.Maintenance, InterventionStatus.Scheduled,
                "Entretien semestriel de la pompe à chaleur", aujourdhui.AddDays(2), new TimeSpan(11, 0, 0), 75),

            // En attente — la file de travail du planificateur.
            CreerIntervention(organisationId, clients[3].Id, null, null,
                InterventionType.Emergency, InterventionStatus.Pending,
                "Plus de chauffage dans l'aile est — signalé ce matin", null, null, 120),
            CreerIntervention(organisationId, clients[2].Id, equipements[3].Id, null,
                InterventionType.Installation, InterventionStatus.Pending,
                "Ajout d'un thermostat connecté", null, null, 45),

            // Annulee — pour que le filtre par statut ait un cas de chaque.
            CreerIntervention(organisationId, clients[0].Id, equipements[0].Id, technicienId,
                InterventionType.Inspection, InterventionStatus.Cancelled,
                "Inspection reportée à la demande du client", aujourdhui.AddDays(-1), new TimeSpan(15, 0, 0), 45)
        };

        // Positions des deux techniciens : l'une pres du chantier en cours,
        // l'autre en deplacement.
        var positions = new[]
        {
            new TechnicianLocation
            {
                Id = Guid.NewGuid(), TechnicianId = technicienneId, OrganizationId = organisationId,
                Latitude = 45.4290, Longitude = -75.7100, Accuracy = 12, Speed = 0, Heading = 0,
                Timestamp = maintenant.AddMinutes(-2), IsOnline = true
            },
            new TechnicianLocation
            {
                Id = Guid.NewGuid(), TechnicianId = technicienId, OrganizationId = organisationId,
                Latitude = 45.3510, Longitude = -75.8200, Accuracy = 18, Speed = 11.4, Heading = 78,
                Timestamp = maintenant.AddMinutes(-1), IsOnline = true
            }
        };

        _context.Clients.AddRange(clients);
        _context.Equipments.AddRange(equipements);
        _context.Interventions.AddRange(interventions);
        _context.TechnicianLocations.AddRange(positions);
    }

    private static Client CreerClient(
        Guid organisationId, string nom, string adresse, string ville, string codePostal,
        double latitude, double longitude, string courriel, string telephone) => new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organisationId,
            Name = nom,
            Address = adresse,
            City = ville,
            PostalCode = codePostal,
            Latitude = latitude,
            Longitude = longitude,
            Email = courriel,
            Phone = telephone
        };

    private static Equipment CreerEquipement(
        Guid organisationId, Guid clientId, EquipmentType type, string marque, string modele,
        DateTime? installation, DateTime? dernierEntretien) => new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organisationId,
            ClientId = clientId,
            Type = type,
            Brand = marque,
            Model = modele,
            SerialNumber = $"DEMO-{Convert.ToHexString(RandomNumberGenerator.GetBytes(3))}",
            InstallationDate = installation,
            LastMaintenanceDate = dernierEntretien
        };

    private static Intervention CreerIntervention(
        Guid organisationId, Guid clientId, Guid? equipementId, Guid? technicienId,
        InterventionType type, InterventionStatus statut, string description,
        DateTime? datePrevue, TimeSpan? heureDebut, int dureeEstimee,
        DateTime? demarre = null, DateTime? termine = null) => new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organisationId,
            ClientId = clientId,
            EquipmentId = equipementId,
            TechnicianId = technicienId,
            Type = type,
            Status = statut,
            Description = description,
            ScheduledDate = datePrevue,
            ScheduledStartTime = heureDebut,
            ScheduledEndTime = heureDebut?.Add(TimeSpan.FromMinutes(dureeEstimee)),
            EstimatedDurationMinutes = dureeEstimee,
            StartedAt = demarre,
            CompletedAt = termine
        };
}
