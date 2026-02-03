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

    public async Task UpdateLocationAsync(Guid technicianId, Guid organizationId, UpdateLocationDto locationDto)
    {
        var existingLocation = await _context.TechnicianLocations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.TechnicianId == technicianId);

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
    }

    public async Task<TechnicianLocationDto?> GetTechnicianLocationAsync(Guid technicianId)
    {
        var location = await _context.TechnicianLocations
            .Include(l => l.Technician)
            .FirstOrDefaultAsync(l => l.TechnicianId == technicianId);

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

    public async Task SetTechnicianOfflineAsync(Guid technicianId)
    {
        var location = await _context.TechnicianLocations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.TechnicianId == technicianId);

        if (location != null)
        {
            location.IsOnline = false;
            await _context.SaveChangesAsync();
        }
    }
}
