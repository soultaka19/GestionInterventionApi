using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.Intervention;

public record UpdateInterventionDto(
    Guid? EquipmentId,
    Guid? TechnicianId,
    InterventionType Type,
    string? Description,
    DateTime? ScheduledDate,
    TimeSpan? ScheduledStartTime,
    TimeSpan? ScheduledEndTime,
    int? EstimatedDurationMinutes,
    string? Notes
);
