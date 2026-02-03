namespace GestionInterventionApi.DTOs.Location;

public record GeocodeResultDto(
    double Latitude,
    double Longitude,
    string FormattedAddress,
    bool Success,
    string? ErrorMessage = null
);
