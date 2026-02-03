using GestionInterventionApi.Models;
using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.Intervention;

public record InterventionDetailDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string ClientAddress,
    string? ClientPhone,
    double? ClientLatitude,
    double? ClientLongitude,
    Guid? EquipmentId,
    string? EquipmentType,
    string? EquipmentBrand,
    string? EquipmentModel,
    Guid? TechnicianId,
    string? TechnicianName,
    string? TechnicianEmail,
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
    string? TechnicianNotes,
    InterventionReport? Report,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
