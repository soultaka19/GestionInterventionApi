namespace GestionInterventionApi.DTOs.Location;

public record RouteOptimizationResultDto(
    List<RouteStepDto> OptimizedRoute,
    double TotalDistanceKm,
    int TotalDurationMinutes,
    bool Success,
    string? ErrorMessage = null
);

public record RouteStepDto(
    int Order,
    CoordinatesDto Location,
    string? Label,
    double DistanceFromPreviousKm,
    int DurationFromPreviousMinutes
);
