using GestionInterventionApi.DTOs.Location;

namespace GestionInterventionApi.Services;

public interface ILocationTrackingService
{
    Task UpdateLocationAsync(Guid technicianId, Guid organizationId, UpdateLocationDto locationDto);
    Task<TechnicianLocationDto?> GetTechnicianLocationAsync(Guid technicianId);
    Task<IEnumerable<TechnicianLocationDto>> GetAllTechnicianLocationsAsync(Guid organizationId);
    Task SetTechnicianOfflineAsync(Guid technicianId);
}
