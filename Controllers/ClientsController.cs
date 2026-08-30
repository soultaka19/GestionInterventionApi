using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionInterventionApi.Data;
using GestionInterventionApi.DTOs.Client;
using GestionInterventionApi.Models;
using GestionInterventionApi.Services;

namespace GestionInterventionApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IGeocodingService _geocodingService;
    private readonly ITenantService _tenantService;

    public ClientsController(
        ApplicationDbContext context,
        IMapper mapper,
        IGeocodingService geocodingService,
        ITenantService tenantService)
    {
        _context = context;
        _mapper = mapper;
        _geocodingService = geocodingService;
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetAll([FromQuery] string? search = null)
    {
        if (!_tenantService.OrganizationId.HasValue)
        {
            return Unauthorized(new { message = "Organisation non identifiée" });
        }

        var query = _context.Clients
            .Include(c => c.Equipments)
            .Where(c => c.OrganizationId == _tenantService.OrganizationId.Value)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(search) ||
                c.Address.ToLower().Contains(search) ||
                (c.City != null && c.City.ToLower().Contains(search)));
        }

        var clients = await query.OrderBy(c => c.Name).ToListAsync();
        return Ok(_mapper.Map<List<ClientDto>>(clients));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientDetailDto>> GetById(Guid id)
    {
        if (!_tenantService.OrganizationId.HasValue)
        {
            return Unauthorized(new { message = "Organisation non identifiée" });
        }

        var client = await _context.Clients
            .Include(c => c.Equipments)
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == _tenantService.OrganizationId.Value);

        if (client == null)
        {
            return NotFound(new { message = "Client non trouvé" });
        }

        return Ok(_mapper.Map<ClientDetailDto>(client));
    }

    [HttpPost]
    [Authorize(Policy = "Gestion")]
    public async Task<ActionResult<ClientDto>> Create([FromBody] CreateClientDto createDto)
    {
        if (!_tenantService.OrganizationId.HasValue)
        {
            return Unauthorized(new { message = "Organisation non identifiée" });
        }

        var 
            client = _mapper.Map<Client>(createDto);
        client.Id = Guid.NewGuid();
        client.OrganizationId = _tenantService.OrganizationId.Value;

        // Géocodage automatique si les coordonnées ne sont pas fournies
        if (!client.Latitude.HasValue || !client.Longitude.HasValue)
        {
            var fullAddress = $"{createDto.Address}, {createDto.PostalCode} {createDto.City}";
            var geocodeResult = await _geocodingService.GeocodeAddressAsync(fullAddress);

            if (geocodeResult.Success)
            {
                client.Latitude = geocodeResult.Latitude;
                client.Longitude = geocodeResult.Longitude;
            }
        }

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        var clientDto = _mapper.Map<ClientDto>(client);
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, clientDto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Gestion")]
    public async Task<ActionResult<ClientDto>> Update(Guid id, [FromBody] UpdateClientDto updateDto)
    {
        if (!_tenantService.OrganizationId.HasValue)
        {
            return Unauthorized(new { message = "Organisation non identifiée" });
        }

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == _tenantService.OrganizationId.Value);

        if (client == null)
        {
            return NotFound(new { message = "Client non trouvé" });
        }

        var addressChanged = client.Address != updateDto.Address ||
                            client.City != updateDto.City ||
                            client.PostalCode != updateDto.PostalCode;

        _mapper.Map(updateDto, client);

        // Re-géocodage si l'adresse a changé et les coordonnées ne sont pas fournies
        if (addressChanged && (!updateDto.Latitude.HasValue || !updateDto.Longitude.HasValue))
        {
            var fullAddress = $"{updateDto.Address}, {updateDto.PostalCode} {updateDto.City}";
            var geocodeResult = await _geocodingService.GeocodeAddressAsync(fullAddress);

            if (geocodeResult.Success)
            {
                client.Latitude = geocodeResult.Latitude;
                client.Longitude = geocodeResult.Longitude;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<ClientDto>(client));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!_tenantService.OrganizationId.HasValue)
        {
            return Unauthorized(new { message = "Organisation non identifiée" });
        }

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == _tenantService.OrganizationId.Value);

        if (client == null)
        {
            return NotFound(new { message = "Client non trouvé" });
        }

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id:guid}/equipments")]
    public async Task<ActionResult<IEnumerable<object>>> GetClientEquipments(Guid id)
    {
        if (!_tenantService.OrganizationId.HasValue)
        {
            return Unauthorized(new { message = "Organisation non identifiée" });
        }

        var client = await _context.Clients
            .Include(c => c.Equipments)
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == _tenantService.OrganizationId.Value);

        if (client == null)
        {
            return NotFound(new { message = "Client non trouvé" });
        }

        return Ok(client.Equipments.Select(e => new
        {
            e.Id,
            e.Type,
            e.Brand,
            e.Model,
            e.SerialNumber,
            e.InstallationDate,
            e.LastMaintenanceDate,
            e.WarrantyEndDate
        }));
    }
}
