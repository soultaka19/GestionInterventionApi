using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.Equipment;

public record EquipmentDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    EquipmentType Type,
    string Brand,
    string? Model,
    string? SerialNumber,
    DateTime? InstallationDate,
    DateTime? LastMaintenanceDate,
    DateTime? WarrantyEndDate,
    string? Notes,
    DateTime CreatedAt
);
