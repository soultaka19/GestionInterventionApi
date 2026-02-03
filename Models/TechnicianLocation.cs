namespace GestionInterventionApi.Models;

public class TechnicianLocation : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TechnicianId { get; set; }
    public Guid OrganizationId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; } // Précision en mètres
    public double? Speed { get; set; } // Vitesse en m/s
    public double? Heading { get; set; } // Direction en degrés
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; } = true;

    // Navigation
    public User Technician { get; set; } = null!;
}
