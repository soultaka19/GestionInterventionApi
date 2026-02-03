using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.Intervention;

public record CreateInterventionDto(
    Guid ClientId,
    Guid? EquipmentId = null,
    Guid? TechnicianId = null,
    InterventionType Type = InterventionType.Maintenance,
    string? Description = null,
    DateTime? ScheduledDate = null,
    TimeSpan? ScheduledStartTime = null,
    TimeSpan? ScheduledEndTime = null,
    int? EstimatedDurationMinutes = null,
    string? Notes = null
);
