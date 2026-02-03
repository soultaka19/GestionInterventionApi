namespace GestionInterventionApi.DTOs.Client;

public record ClientDto(
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
    int EquipmentCount, // Doit correspondre au nom dans ForCtorParam
    DateTime CreatedAt
);