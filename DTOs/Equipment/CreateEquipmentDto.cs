using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.Equipment;

public record CreateEquipmentDto(
    Guid ClientId,
    EquipmentType Type,
    string Brand,
    string? Model = null,
    string? SerialNumber = null,
    DateTime? InstallationDate = null,
    DateTime? WarrantyEndDate = null,
    string? Notes = null
);
