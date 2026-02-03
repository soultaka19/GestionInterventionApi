using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using GestionInterventionApi.DTOs.Location;
using GestionInterventionApi.Services;

namespace GestionInterventionApi.Hubs;

[Authorize]
public class LocationHub : Hub
{
    private readonly ILocationTrackingService _locationService;
    private readonly ILogger<LocationHub> _logger;

    public LocationHub(ILocationTrackingService locationService, ILogger<LocationHub> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var organizationId = GetOrganizationId();
        if (organizationId.HasValue)
        {
            // Ajouter l'utilisateur au groupe de son organisation
            await Groups.AddToGroupAsync(Context.ConnectionId, $"org_{organizationId}");
            _logger.LogInformation("User {UserId} connected to organization group {OrgId}",
                Context.UserIdentifier, organizationId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            await _locationService.SetTechnicianOfflineAsync(userId.Value);

            var organizationId = GetOrganizationId();
            if (organizationId.HasValue)
            {
                // Notifier les autres utilisateurs que le technicien est hors ligne
                await Clients.Group($"org_{organizationId}")
                    .SendAsync("TechnicianOffline", userId.Value);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Appelé par les techniciens pour envoyer leur position (toutes les minutes)
    /// </summary>
    public async Task UpdateLocation(UpdateLocationDto locationDto)
    {
        var userId = GetUserId();
        var organizationId = GetOrganizationId();

        if (!userId.HasValue || !organizationId.HasValue)
        {
            _logger.LogWarning("UpdateLocation called without valid user context");
            return;
        }

        // Sauvegarder la position
        await _locationService.UpdateLocationAsync(userId.Value, organizationId.Value, locationDto);

        // Récupérer les infos complètes pour broadcast
        var technicianLocation = await _locationService.GetTechnicianLocationAsync(userId.Value);

        if (technicianLocation != null)
        {
            // Envoyer la position à tous les membres de l'organisation (planificateurs + clients)
            await Clients.Group($"org_{organizationId}")
                .SendAsync("LocationUpdated", technicianLocation);
        }
    }

    /// <summary>
    /// Permet aux clients de s'abonner aux mises à jour d'un technicien spécifique
    /// </summary>
    public async Task SubscribeToTechnician(Guid technicianId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tech_{technicianId}");
        _logger.LogInformation("User {UserId} subscribed to technician {TechnicianId}",
            Context.UserIdentifier, technicianId);

        // Envoyer la dernière position connue
        var location = await _locationService.GetTechnicianLocationAsync(technicianId);
        if (location != null)
        {
            await Clients.Caller.SendAsync("LocationUpdated", location);
        }
    }

    /// <summary>
    /// Se désabonner des mises à jour d'un technicien
    /// </summary>
    public async Task UnsubscribeFromTechnician(Guid technicianId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tech_{technicianId}");
    }

    /// <summary>
    /// Récupérer toutes les positions des techniciens de l'organisation
    /// </summary>
    public async Task GetAllTechnicianLocations()
    {
        var organizationId = GetOrganizationId();
        if (!organizationId.HasValue)
        {
            await Clients.Caller.SendAsync("Error", "Organization not found");
            return;
        }

        var locations = await _locationService.GetAllTechnicianLocationsAsync(organizationId.Value);
        await Clients.Caller.SendAsync("AllLocations", locations);
    }

    private Guid? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private Guid? GetOrganizationId()
    {
        var orgIdClaim = Context.User?.FindFirst("OrganizationId")?.Value;
        return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : null;
    }
}
