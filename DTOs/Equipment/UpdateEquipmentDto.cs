using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.Equipment;

public record UpdateEquipmentDto(
    EquipmentType Type,
    string Brand,
    string? Model,
    string? SerialNumber,
    DateTime? InstallationDate,
    DateTime? LastMaintenanceDate,
    DateTime? WarrantyEndDate,
    string? Notes
);
