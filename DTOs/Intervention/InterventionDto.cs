using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.Intervention;

public record InterventionDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string ClientAddress,
    double? ClientLatitude,
    double? ClientLongitude,
    Guid? EquipmentId,
    string? EquipmentInfo,
    Guid? TechnicianId,
    string? TechnicianName,
    InterventionType Type,
    InterventionStatus Status,
    string? Description,
    DateTime? ScheduledDate,
    TimeSpan? ScheduledStartTime,
    TimeSpan? ScheduledEndTime,
    int? EstimatedDurationMinutes,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? Notes,
    DateTime CreatedAt
);
