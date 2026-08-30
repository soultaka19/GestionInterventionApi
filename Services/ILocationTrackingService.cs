using GestionInterventionApi.DTOs.Location;

namespace GestionInterventionApi.Services;

public interface ILocationTrackingService
{
    /// <summary>
    /// Ecrit la position d'un technicien. Renvoie <c>false</c> si le technicien
    /// n'appartient pas a <paramref name="organizationId"/> — l'appelant doit
    /// alors repondre 404, jamais 200.
    /// </summary>
    Task<bool> UpdateLocationAsync(Guid technicianId, Guid organizationId, UpdateLocationDto locationDto);

    /// <summary>
    /// Position d'un technicien, restreinte a une organisation.
    /// <paramref name="organizationId"/> est un parametre OBLIGATOIRE et non une
    /// commodite : la methode etait auparavant filtree par le seul filtre global
    /// du DbContext, lui-meme inoperant dans un scope SignalR.
    /// </summary>
    Task<TechnicianLocationDto?> GetTechnicianLocationAsync(Guid technicianId, Guid organizationId);

    Task<IEnumerable<TechnicianLocationDto>> GetAllTechnicianLocationsAsync(Guid organizationId);

    Task SetTechnicianOfflineAsync(Guid technicianId, Guid organizationId);
}
