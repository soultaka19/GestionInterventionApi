namespace GestionInterventionApi.DTOs.Intervention;

public record AssignTechnicianDto(
    Guid TechnicianId,
    DateTime? ScheduledDate = null,
    TimeSpan? ScheduledStartTime = null,
    TimeSpan? ScheduledEndTime = null
);
