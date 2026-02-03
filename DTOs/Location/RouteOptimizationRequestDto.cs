namespace GestionInterventionApi.DTOs.Location;

public record RouteOptimizationRequestDto(
    CoordinatesDto Origin,
    List<CoordinatesDto> Waypoints,
    CoordinatesDto? Destination = null
);

public record CoordinatesDto(
    double Latitude,
    double Longitude,
    string? Label = null
);
