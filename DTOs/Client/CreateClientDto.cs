namespace GestionInterventionApi.DTOs.Client;

public record CreateClientDto(
    string Name,
    string Address,
    string? City = null,
    string? PostalCode = null,
    string? Phone = null,
    string? Email = null,
    double? Latitude = null,
    double? Longitude = null,
    string? Notes = null
);
