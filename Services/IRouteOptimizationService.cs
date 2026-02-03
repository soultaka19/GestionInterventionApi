using GestionInterventionApi.DTOs.Location;

namespace GestionInterventionApi.Services;

public interface IRouteOptimizationService
{
    Task<RouteOptimizationResultDto> OptimizeRouteAsync(RouteOptimizationRequestDto request);
    Task<(double DistanceKm, int DurationMinutes)> GetDistanceAndDurationAsync(
        CoordinatesDto origin,
        CoordinatesDto destination);
}
