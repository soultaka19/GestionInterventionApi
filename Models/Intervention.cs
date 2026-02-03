using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.Models;

public class Intervention : BaseEntity
{
    public Guid ClientId { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid? TechnicianId { get; set; }

    public InterventionType Type { get; set; }
    public InterventionStatus Status { get; set; } = InterventionStatus.Pending;
    public string? Description { get; set; }

    // Planification
    public DateTime? ScheduledDate { get; set; }
    public TimeSpan? ScheduledStartTime { get; set; }
    public TimeSpan? ScheduledEndTime { get; set; }
    public int? EstimatedDurationMinutes { get; set; }

    // Exécution
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Rapport d'intervention (JSON)
    public string? ReportJson { get; set; }

    // Notes et commentaires
    public string? Notes { get; set; }
    public string? TechnicianNotes { get; set; }

    // Navigation
    public Client Client { get; set; } = null!;
    public Equipment? Equipment { get; set; }
    public User? Technician { get; set; }
    public Organization Organization { get; set; } = null!;
}
