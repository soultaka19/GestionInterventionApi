using Microsoft.EntityFrameworkCore;
using GestionInterventionApi.Data;
using GestionInterventionApi.DTOs.Location;
using GestionInterventionApi.Models;

namespace GestionInterventionApi.Services;

public class LocationTrackingService : ILocationTrackingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LocationTrackingService> _logger;

    public LocationTrackingService(ApplicationDbContext context, ILogger<LocationTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> UpdateLocationAsync(Guid technicianId, Guid organizationId, UpdateLocationDto locationDto)
    {
        // B-3 — le technicien vise doit appartenir a l'organisation de l'appelant.
        //
        // Avant : la ligne de position etait cherchee par TechnicianId SEUL, avec
        // IgnoreQueryFilters(). Un Admin pouvait donc ecraser la position d'un
        // technicien d'une autre organisation en passant son GUID ; et si aucune
        // ligne n'existait, on inserait une position portant l'OrganizationId de
        // l'appelant pour un utilisateur etranger. Cette ligne orpheline faisait
        // ensuite echouer GetAllTechnicianLocationsAsync (Technician null apres
        // Include filtre) : un 500 pour toute l'organisation.
        var appartient = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Id == technicianId && u.OrganizationId == organizationId);

        if (!appartient)
        {
            _logger.LogWarning(
                "Tentative d'ecriture de position sur le technicien {TechnicianId}, "
                + "hors de l'organisation {OrganizationId} — refusee",
                technicianId, organizationId);
            return false;
        }

        var existingLocation = await _context.TechnicianLocations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.TechnicianId == technicianId
                                   && l.OrganizationId == organizationId);

        if (existingLocation != null)
        {
            existingLocation.Latitude = locationDto.Latitude;
            existingLocation.Longitude = locationDto.Longitude;
            existingLocation.Accuracy = locationDto.Accuracy;
            existingLocation.Speed = locationDto.Speed;
            existingLocation.Heading = locationDto.Heading;
            existingLocation.Timestamp = DateTime.UtcNow;
            existingLocation.IsOnline = true;
        }
        else
        {
            var newLocation = new TechnicianLocation
            {
                Id = Guid.NewGuid(),
                TechnicianId = technicianId,
                OrganizationId = organizationId,
                Latitude = locationDto.Latitude,
                Longitude = locationDto.Longitude,
                Accuracy = locationDto.Accuracy,
                Speed = locationDto.Speed,
                Heading = locationDto.Heading,
                IsOnline = true
            };
            _context.TechnicianLocations.Add(newLocation);
        }

        await _context.SaveChangesAsync();
        _logger.LogDebug("Location updated for technician {TechnicianId}", technicianId);
        return true;
    }

    public async Task<TechnicianLocationDto?> GetTechnicianLocationAsync(Guid technicianId, Guid organizationId)
    {
        // B-2 — filtre d'organisation EXPLICITE, en plus du filtre global du
        // DbContext. Le filtre global ne s'applique qu'a condition que le tenant
        // soit renseigne dans le scope courant ; l'ecrire ici rend la garantie
        // independante de cette condition.
        var location = await _context.TechnicianLocations
            .Include(l => l.Technician)
            .FirstOrDefaultAsync(l => l.TechnicianId == technicianId
                                   && l.OrganizationId == organizationId);

        if (location == null) return null;

        return new TechnicianLocationDto(
            location.TechnicianId,
            $"{location.Technician.FirstName} {location.Technician.LastName}",
            location.Latitude,
            location.Longitude,
            location.Accuracy,
            location.Speed,
            location.Heading,
            location.Timestamp,
            location.IsOnline
        );
    }

    public async Task<IEnumerable<TechnicianLocationDto>> GetAllTechnicianLocationsAsync(Guid organizationId)
    {
        var locations = await _context.TechnicianLocations
            .Include(l => l.Technician)
            .Where(l => l.OrganizationId == organizationId)
            .ToListAsync();

        return locations.Select(l => new TechnicianLocationDto(
            l.TechnicianId,
            $"{l.Technician.FirstName} {l.Technician.LastName}",
            l.Latitude,
            l.Longitude,
            l.Accuracy,
            l.Speed,
            l.Heading,
            l.Timestamp,
            l.IsOnline
        ));
    }

    public async Task SetTechnicianOfflineAsync(Guid technicianId, Guid organizationId)
    {
        var location = await _context.TechnicianLocations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.TechnicianId == technicianId
                                   && l.OrganizationId == organizationId);

        if (location != null)
        {
            location.IsOnline = false;
            await _context.SaveChangesAsync();
        }
    }
}
