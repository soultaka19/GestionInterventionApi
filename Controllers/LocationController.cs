using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GestionInterventionApi.DTOs.Location;
using GestionInterventionApi.Services;

namespace GestionInterventionApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly ILocationTrackingService _locationService;
    private readonly IGeocodingService _geocodingService;
    private readonly IRouteOptimizationService _routeService;

    public LocationController(
        ILocationTrackingService locationService,
        IGeocodingService geocodingService,
        IRouteOptimizationService routeService)
    {
        _locationService = locationService;
        _geocodingService = geocodingService;
        _routeService = routeService;
    }

    /// <summary>
    /// Met à jour la position du technicien connecté
    /// </summary>
    [HttpPost("update")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationDto locationDto)
    {
        var userId = GetUserId();
        var orgId = GetOrganizationId();

        if (!userId.HasValue || !orgId.HasValue)
        {
            return Unauthorized();
        }

        await _locationService.UpdateLocationAsync(userId.Value, orgId.Value, locationDto);
        return Ok(new { message = "Location updated" });
    }

    /// <summary>
    /// Récupère la position d'un technicien
    /// </summary>
    [HttpGet("technician/{technicianId:guid}")]
    public async Task<ActionResult<TechnicianLocationDto>> GetTechnicianLocation(Guid technicianId)
    {
        var location = await _locationService.GetTechnicianLocationAsync(technicianId);

        if (location == null)
        {
            return NotFound(new { message = "Technician location not found" });
        }

        return Ok(location);
    }

    /// <summary>
    /// Récupère toutes les positions des techniciens de l'organisation
    /// </summary>
    [HttpGet("technicians")]
    public async Task<ActionResult<IEnumerable<TechnicianLocationDto>>> GetAllTechnicianLocations()
    {
        var orgId = GetOrganizationId();

        if (!orgId.HasValue)
        {
            return Unauthorized();
        }

        var locations = await _locationService.GetAllTechnicianLocationsAsync(orgId.Value);
        return Ok(locations);
    }

    /// <summary>
    /// Géocode une adresse en coordonnées GPS
    /// </summary>
    [HttpGet("geocode")]
    public async Task<ActionResult<GeocodeResultDto>> GeocodeAddress([FromQuery] string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return BadRequest(new { message = "Address is required" });
        }

        var result = await _geocodingService.GeocodeAddressAsync(address);
        return Ok(result);
    }

    /// <summary>
    /// Reverse geocode des coordonnées en adresse
    /// </summary>
    [HttpGet("reverse-geocode")]
    public async Task<ActionResult<object>> ReverseGeocode([FromQuery] double latitude, [FromQuery] double longitude)
    {
        var address = await _geocodingService.ReverseGeocodeAsync(latitude, longitude);
        return Ok(new { address });
    }

    /// <summary>
    /// Optimise un itinéraire entre plusieurs points
    /// </summary>
    [HttpPost("optimize-route")]
    public async Task<ActionResult<RouteOptimizationResultDto>> OptimizeRoute([FromBody] RouteOptimizationRequestDto request)
    {
        if (request.Waypoints == null || request.Waypoints.Count == 0)
        {
            return BadRequest(new { message = "At least one waypoint is required" });
        }

        var result = await _routeService.OptimizeRouteAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Calcule la distance et la durée entre deux points
    /// </summary>
    [HttpGet("distance")]
    public async Task<ActionResult<object>> GetDistance(
        [FromQuery] double originLat,
        [FromQuery] double originLng,
        [FromQuery] double destLat,
        [FromQuery] double destLng)
    {
        var origin = new CoordinatesDto(originLat, originLng);
        var destination = new CoordinatesDto(destLat, destLng);

        var (distanceKm, durationMinutes) = await _routeService.GetDistanceAndDurationAsync(origin, destination);

        return Ok(new { distanceKm, durationMinutes });
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private Guid? GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
        return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : null;
    }
}
