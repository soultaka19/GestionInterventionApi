using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionInterventionApi.Data;
using GestionInterventionApi.DTOs.Equipment;
using GestionInterventionApi.Models;
using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EquipmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public EquipmentsController(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EquipmentDto>>> GetAll(
        [FromQuery] Guid? clientId = null,
        [FromQuery] EquipmentType? type = null,
        [FromQuery] string? search = null)
    {
        var query = _context.Equipments
            .Include(e => e.Client)
            .AsQueryable();

        if (clientId.HasValue)
        {
            query = query.Where(e => e.ClientId == clientId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(e => e.Type == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(e =>
                e.Brand.ToLower().Contains(search) ||
                (e.Model != null && e.Model.ToLower().Contains(search)) ||
                (e.SerialNumber != null && e.SerialNumber.ToLower().Contains(search)) ||
                e.Client.Name.ToLower().Contains(search));
        }

        var equipments = await query.OrderBy(e => e.Client.Name).ThenBy(e => e.Type).ToListAsync();
        return Ok(_mapper.Map<List<EquipmentDto>>(equipments));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EquipmentDto>> GetById(Guid id)
    {
        var equipment = await _context.Equipments
            .Include(e => e.Client)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (equipment == null)
        {
            return NotFound(new { message = "Équipement non trouvé" });
        }

        return Ok(_mapper.Map<EquipmentDto>(equipment));
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentDto>> Create([FromBody] CreateEquipmentDto createDto)
    {
        // Vérifier que le client existe
        var clientExists = await _context.Clients.AnyAsync(c => c.Id == createDto.ClientId);
        if (!clientExists)
        {
            return BadRequest(new { message = "Client non trouvé" });
        }

        var equipment = _mapper.Map<Equipment>(createDto);
        equipment.Id = Guid.NewGuid();

        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync();

        // Recharger avec le Client pour le mapping
        await _context.Entry(equipment).Reference(e => e.Client).LoadAsync();

        var equipmentDto = _mapper.Map<EquipmentDto>(equipment);
        return CreatedAtAction(nameof(GetById), new { id = equipment.Id }, equipmentDto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EquipmentDto>> Update(Guid id, [FromBody] UpdateEquipmentDto updateDto)
    {
        var equipment = await _context.Equipments
            .Include(e => e.Client)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (equipment == null)
        {
            return NotFound(new { message = "Équipement non trouvé" });
        }

        _mapper.Map(updateDto, equipment);
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<EquipmentDto>(equipment));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var equipment = await _context.Equipments.FindAsync(id);

        if (equipment == null)
        {
            return NotFound(new { message = "Équipement non trouvé" });
        }

        _context.Equipments.Remove(equipment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:guid}/maintenance")]
    public async Task<ActionResult<EquipmentDto>> UpdateMaintenanceDate(Guid id)
    {
        var equipment = await _context.Equipments
            .Include(e => e.Client)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (equipment == null)
        {
            return NotFound(new { message = "Équipement non trouvé" });
        }

        equipment.LastMaintenanceDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<EquipmentDto>(equipment));
    }

    [HttpGet("types")]
    public ActionResult<IEnumerable<object>> GetEquipmentTypes()
    {
        var types = Enum.GetValues<EquipmentType>()
            .Select(t => new
            {
                Value = (int)t,
                Name = t.ToString()
            });

        return Ok(types);
    }

    [HttpGet("warranty-expiring")]
    public async Task<ActionResult<IEnumerable<EquipmentDto>>> GetWarrantyExpiring([FromQuery] int daysAhead = 30)
    {
        var expirationDate = DateTime.UtcNow.AddDays(daysAhead);

        var equipments = await _context.Equipments
            .Include(e => e.Client)
            .Where(e => e.WarrantyEndDate.HasValue &&
                       e.WarrantyEndDate.Value <= expirationDate &&
                       e.WarrantyEndDate.Value >= DateTime.UtcNow)
            .OrderBy(e => e.WarrantyEndDate)
            .ToListAsync();

        return Ok(_mapper.Map<List<EquipmentDto>>(equipments));
    }

    [HttpGet("maintenance-due")]
    public async Task<ActionResult<IEnumerable<EquipmentDto>>> GetMaintenanceDue([FromQuery] int monthsSinceLastMaintenance = 12)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-monthsSinceLastMaintenance);

        var equipments = await _context.Equipments
            .Include(e => e.Client)
            .Where(e => e.LastMaintenanceDate == null || e.LastMaintenanceDate.Value <= cutoffDate)
            .OrderBy(e => e.LastMaintenanceDate)
            .ToListAsync();

        return Ok(_mapper.Map<List<EquipmentDto>>(equipments));
    }
}
