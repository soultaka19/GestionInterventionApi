using GestionInterventionApi.DTOs.Equipment;

namespace GestionInterventionApi.DTOs.Client;

public record ClientDetailDto(
    Guid Id,
    string Name,
    string Address,
    string? City,
    string? PostalCode,
    string? Phone,
    string? Email,
    double? Latitude,
    double? Longitude,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<EquipmentDto> Equipments
);
