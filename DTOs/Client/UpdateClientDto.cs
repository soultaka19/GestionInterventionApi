namespace GestionInterventionApi.DTOs.Client;

public record UpdateClientDto(
    string Name,
    string Address,
    string? City,
    string? PostalCode,
    string? Phone,
    string? Email,
    double? Latitude,
    double? Longitude,
    string? Notes
);
