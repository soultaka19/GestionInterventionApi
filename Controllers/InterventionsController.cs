using System.Security.Claims;
using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionInterventionApi.Data;
using GestionInterventionApi.DTOs.Intervention;
using GestionInterventionApi.Models;
using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InterventionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public InterventionsController(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InterventionDto>>> GetAll(
        [FromQuery] InterventionStatus? status = null,
        [FromQuery] Guid? technicianId = null,
        [FromQuery] Guid? clientId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = _context.Interventions
            .Include(i => i.Client)
            .Include(i => i.Equipment)
            .Include(i => i.Technician)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (technicianId.HasValue)
            query = query.Where(i => i.TechnicianId == technicianId.Value);

        if (clientId.HasValue)
            query = query.Where(i => i.ClientId == clientId.Value);

        if (fromDate.HasValue)
            query = query.Where(i => i.ScheduledDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(i => i.ScheduledDate <= toDate.Value);

        var interventions = await query
            .OrderByDescending(i => i.ScheduledDate ?? i.CreatedAt)
            .ToListAsync();

        return Ok(interventions.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InterventionDetailDto>> GetById(Guid id)
    {
        var intervention = await _context.Interventions
            .Include(i => i.Client)
            .Include(i => i.Equipment)
            .Include(i => i.Technician)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (intervention == null)
            return NotFound(new { message = "Intervention non trouvée" });

        return Ok(MapToDetailDto(intervention));
    }

    [HttpPost]
    [Authorize(Policy = "Gestion")]
    public async Task<ActionResult<InterventionDto>> Create([FromBody] CreateInterventionDto createDto)
    {
        // Vérifier que le client existe
        var clientExists = await _context.Clients.AnyAsync(c => c.Id == createDto.ClientId);
        if (!clientExists)
            return BadRequest(new { message = "Client non trouvé" });

        // Vérifier l'équipement si fourni
        if (createDto.EquipmentId.HasValue)
        {
            var equipmentExists = await _context.Equipments
                .AnyAsync(e => e.Id == createDto.EquipmentId.Value && e.ClientId == createDto.ClientId);
            if (!equipmentExists)
                return BadRequest(new { message = "Équipement non trouvé ou n'appartient pas au client" });
        }

        // Vérifier le technicien si fourni
        if (createDto.TechnicianId.HasValue)
        {
            var technicianExists = await _context.Users
                .AnyAsync(u => u.Id == createDto.TechnicianId.Value && u.Role == UserRole.Technicien);
            if (!technicianExists)
                return BadRequest(new { message = "Technicien non trouvé" });
        }

        var intervention = new Intervention
        {
            Id = Guid.NewGuid(),
            ClientId = createDto.ClientId,
            EquipmentId = createDto.EquipmentId,
            TechnicianId = createDto.TechnicianId,
            Type = createDto.Type,
            Status = createDto.TechnicianId.HasValue && createDto.ScheduledDate.HasValue
                ? InterventionStatus.Scheduled
                : InterventionStatus.Pending,
            Description = createDto.Description,
            ScheduledDate = createDto.ScheduledDate,
            ScheduledStartTime = createDto.ScheduledStartTime,
            ScheduledEndTime = createDto.ScheduledEndTime,
            EstimatedDurationMinutes = createDto.EstimatedDurationMinutes,
            Notes = createDto.Notes
        };

        _context.Interventions.Add(intervention);
        await _context.SaveChangesAsync();

        await _context.Entry(intervention).Reference(i => i.Client).LoadAsync();
        await _context.Entry(intervention).Reference(i => i.Equipment).LoadAsync();
        await _context.Entry(intervention).Reference(i => i.Technician).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = intervention.Id }, MapToDto(intervention));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Gestion")]
    public async Task<ActionResult<InterventionDto>> Update(Guid id, [FromBody] UpdateInterventionDto updateDto)
    {
        var intervention = await _context.Interventions
            .Include(i => i.Client)
            .Include(i => i.Equipment)
            .Include(i => i.Technician)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (intervention == null)
            return NotFound(new { message = "Intervention non trouvée" });

        if (intervention.Status == InterventionStatus.Completed || intervention.Status == InterventionStatus.Cancelled)
            return BadRequest(new { message = "Impossible de modifier une intervention terminée ou annulée" });

        // F-3 / B-7 — une mise a jour n'efface plus ce qu'elle ne mentionne pas.
        //
        // Chaque champ etait recopie tel quel, y compris a null. Le formulaire
        // d'edition du front n'envoie ni technicianId ni planification :
        // corriger une simple faute de frappe dans la description desaffectait
        // le technicien et effacait la date, l'heure de debut et l'heure de fin.
        // L'intervention repassait donc silencieusement de « Planifiee » a
        // « En attente », sans qu'aucune erreur ne soit affichee.
        //
        // Contrepartie assumee, identique a celle du mapping AutoMapper : vider
        // un champ optionnel demande une action dediee (desaffectation), non un
        // null glisse dans une mise a jour.

        // Les references changees doivent rester dans l'organisation, comme a la
        // creation — l'audit relevait qu'Update ne faisait aucun de ces controles.
        if (updateDto.EquipmentId.HasValue && updateDto.EquipmentId != intervention.EquipmentId)
        {
            var equipementValide = await _context.Equipments
                .AnyAsync(e => e.Id == updateDto.EquipmentId.Value && e.ClientId == intervention.ClientId);
            if (!equipementValide)
                return BadRequest(new { message = "Équipement non trouvé ou n'appartient pas au client" });
        }

        if (updateDto.TechnicianId.HasValue && updateDto.TechnicianId != intervention.TechnicianId)
        {
            var technicienValide = await _context.Users
                .AnyAsync(u => u.Id == updateDto.TechnicianId.Value && u.Role == UserRole.Technicien);
            if (!technicienValide)
                return BadRequest(new { message = "Technicien non trouvé" });
        }

        if (updateDto.EquipmentId.HasValue)
            intervention.EquipmentId = updateDto.EquipmentId;
        if (updateDto.TechnicianId.HasValue)
            intervention.TechnicianId = updateDto.TechnicianId;
        // Type est un enum non nullable : toujours fourni par le formulaire.
        intervention.Type = updateDto.Type;
        if (updateDto.Description is not null)
            intervention.Description = updateDto.Description;
        if (updateDto.ScheduledDate.HasValue)
            intervention.ScheduledDate = updateDto.ScheduledDate;
        if (updateDto.ScheduledStartTime.HasValue)
            intervention.ScheduledStartTime = updateDto.ScheduledStartTime;
        if (updateDto.ScheduledEndTime.HasValue)
            intervention.ScheduledEndTime = updateDto.ScheduledEndTime;
        if (updateDto.EstimatedDurationMinutes.HasValue)
            intervention.EstimatedDurationMinutes = updateDto.EstimatedDurationMinutes;
        if (updateDto.Notes is not null)
            intervention.Notes = updateDto.Notes;

        await _context.SaveChangesAsync();

        return Ok(MapToDto(intervention));
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = "Gestion")]
    public async Task<ActionResult<InterventionDto>> AssignTechnician(Guid id, [FromBody] AssignTechnicianDto assignDto)
    {
        var intervention = await _context.Interventions
            .Include(i => i.Client)
            .Include(i => i.Equipment)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (intervention == null)
            return NotFound(new { message = "Intervention non trouvée" });

        var technician = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == assignDto.TechnicianId && u.Role == UserRole.Technicien);

        if (technician == null)
            return BadRequest(new { message = "Technicien non trouvé" });

        intervention.TechnicianId = assignDto.TechnicianId;
        intervention.Technician = technician;

        if (assignDto.ScheduledDate.HasValue)
            intervention.ScheduledDate = assignDto.ScheduledDate;

        if (assignDto.ScheduledStartTime.HasValue)
            intervention.ScheduledStartTime = assignDto.ScheduledStartTime;

        if (assignDto.ScheduledEndTime.HasValue)
            intervention.ScheduledEndTime = assignDto.ScheduledEndTime;

        if (intervention.ScheduledDate.HasValue)
            intervention.Status = InterventionStatus.Scheduled;

        await _context.SaveChangesAsync();

        return Ok(MapToDto(intervention));
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<InterventionDto>> StartIntervention(Guid id)
    {
        var intervention = await _context.Interventions
            .Include(i => i.Client)
            .Include(i => i.Equipment)
            .Include(i => i.Technician)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (intervention == null)
            return NotFound(new { message = "Intervention non trouvée" });

        if (intervention.Status != InterventionStatus.Scheduled && intervention.Status != InterventionStatus.Pending)
            return BadRequest(new { message = "L'intervention ne peut pas être démarrée" });

        // Vérifier que c'est le bon technicien ou un Admin/Planificateur
        var userId = GetUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var isAdminOrPlanificateur = userRole == nameof(UserRole.Admin) || userRole == nameof(UserRole.Planificateur);
        if (intervention.TechnicianId != userId && !isAdminOrPlanificateur)
            return Forbid();

        intervention.Status = InterventionStatus.InProgress;
        intervention.StartedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(MapToDto(intervention));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<InterventionDto>> CompleteIntervention(Guid id, [FromBody] CompleteInterventionDto completeDto)
    {
        var intervention = await _context.Interventions
            .Include(i => i.Client)
            .Include(i => i.Equipment)
            .Include(i => i.Technician)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (intervention == null)
            return NotFound(new { message = "Intervention non trouvée" });

        if (intervention.Status != InterventionStatus.InProgress)
            return BadRequest(new { message = "L'intervention doit être en cours pour être complétée" });

        // Vérifier que c'est le bon technicien ou un Admin/Planificateur
        var userId = GetUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var isAdminOrPlanificateur = userRole == nameof(UserRole.Admin) || userRole == nameof(UserRole.Planificateur);
        if (intervention.TechnicianId != userId && !isAdminOrPlanificateur)
            return Forbid();

        intervention.Status = InterventionStatus.Completed;
        intervention.CompletedAt = DateTime.UtcNow;
        intervention.TechnicianNotes = completeDto.TechnicianNotes;

        if (completeDto.Report != null)
            intervention.ReportJson = JsonSerializer.Serialize(completeDto.Report);

        // Mettre à jour la date de maintenance de l'équipement si applicable
        if (intervention.EquipmentId.HasValue && intervention.Type == InterventionType.Maintenance)
        {
            var equipment = await _context.Equipments.FindAsync(intervention.EquipmentId.Value);
            if (equipment != null)
            {
                equipment.LastMaintenanceDate = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(MapToDto(intervention));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Gestion")]
    public async Task<ActionResult<InterventionDto>> CancelIntervention(Guid id, [FromBody] CancelInterventionDto? corps = null)
    {
        var intervention = await _context.Interventions
            .Include(i => i.Client)
            .Include(i => i.Equipment)
            .Include(i => i.Technician)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (intervention == null)
            return NotFound(new { message = "Intervention non trouvée" });

        if (intervention.Status == InterventionStatus.Completed)
            return BadRequest(new { message = "Impossible d'annuler une intervention terminée" });

        intervention.Status = InterventionStatus.Cancelled;
        var reason = corps?.Reason;
        if (!string.IsNullOrEmpty(reason))
            intervention.Notes = (intervention.Notes ?? "") + $"\n[Annulation] {reason}";

        await _context.SaveChangesAsync();

        return Ok(MapToDto(intervention));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Gestion")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var intervention = await _context.Interventions.FindAsync(id);

        if (intervention == null)
            return NotFound(new { message = "Intervention non trouvée" });

        if (intervention.Status == InterventionStatus.InProgress || intervention.Status == InterventionStatus.Completed)
            return BadRequest(new { message = "Impossible de supprimer une intervention en cours ou terminée" });

        _context.Interventions.Remove(intervention);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("my-interventions")]
    public async Task<ActionResult<IEnumerable<InterventionDto>>> GetMyInterventions(
        [FromQuery] DateTime? date = null,
        [FromQuery] InterventionStatus? status = null)
    {
        var userId = GetUserId();

        var query = _context.Interventions
            .Include(i => i.Client)
            .Include(i => i.Equipment)
            .Where(i => i.TechnicianId == userId);

        if (date.HasValue)
            query = query.Where(i => i.ScheduledDate == date.Value.Date);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        var interventions = await query
            .OrderBy(i => i.ScheduledDate)
            .ThenBy(i => i.ScheduledStartTime)
            .ToListAsync();

        return Ok(interventions.Select(MapToDto));
    }

    [HttpGet("planning")]
    public async Task<ActionResult<object>> GetPlanning(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] Guid? technicianId = null)
    {
        var query = _context.Interventions
            .Include(i => i.Client)
            .Include(i => i.Equipment)
            .Include(i => i.Technician)
            .Where(i => i.ScheduledDate >= startDate.Date && i.ScheduledDate <= endDate.Date);

        if (technicianId.HasValue)
            query = query.Where(i => i.TechnicianId == technicianId.Value);

        var interventions = await query
            .OrderBy(i => i.ScheduledDate)
            .ThenBy(i => i.ScheduledStartTime)
            .ToListAsync();

        return Ok(new
        {
            StartDate = startDate.ToString("yyyy-MM-dd"),
            EndDate = endDate.ToString("yyyy-MM-dd"),
            Interventions = interventions.Select(MapToDto),
            TotalCount = interventions.Count
        });
    }

    [HttpGet("statuses")]
    public ActionResult<IEnumerable<object>> GetStatuses()
    {
        var statuses = Enum.GetValues<InterventionStatus>()
            .Select(s => new { Value = (int)s, Name = s.ToString() });
        return Ok(statuses);
    }

    [HttpGet("types")]
    public ActionResult<IEnumerable<object>> GetTypes()
    {
        var types = Enum.GetValues<InterventionType>()
            .Select(t => new { Value = (int)t, Name = t.ToString() });
        return Ok(types);
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private InterventionDto MapToDto(Intervention i)
    {
        return new InterventionDto(
            i.Id,
            i.ClientId,
            i.Client.Name,
            i.Client.Address,
            i.Client.Latitude,
            i.Client.Longitude,
            i.EquipmentId,
            i.Equipment != null ? $"{i.Equipment.Type} - {i.Equipment.Brand}" : null,
            i.TechnicianId,
            i.Technician != null ? $"{i.Technician.FirstName} {i.Technician.LastName}" : null,
            i.Type,
            i.Status,
            i.Description,
            i.ScheduledDate,
            i.ScheduledStartTime,
            i.ScheduledEndTime,
            i.EstimatedDurationMinutes,
            i.StartedAt,
            i.CompletedAt,
            i.Notes,
            i.CreatedAt
        );
    }

    private InterventionDetailDto MapToDetailDto(Intervention i)
    {
        InterventionReport? report = null;
        if (!string.IsNullOrEmpty(i.ReportJson))
        {
            try
            {
                report = JsonSerializer.Deserialize<InterventionReport>(i.ReportJson);
            }
            catch { }
        }

        return new InterventionDetailDto(
            i.Id,
            i.ClientId,
            i.Client.Name,
            i.Client.Address,
            i.Client.Phone,
            i.Client.Latitude,
            i.Client.Longitude,
            i.EquipmentId,
            i.Equipment?.Type.ToString(),
            i.Equipment?.Brand,
            i.Equipment?.Model,
            i.TechnicianId,
            i.Technician != null ? $"{i.Technician.FirstName} {i.Technician.LastName}" : null,
            i.Technician?.Email,
            i.Type,
            i.Status,
            i.Description,
            i.ScheduledDate,
            i.ScheduledStartTime,
            i.ScheduledEndTime,
            i.EstimatedDurationMinutes,
            i.StartedAt,
            i.CompletedAt,
            i.Notes,
            i.TechnicianNotes,
            report,
            i.CreatedAt,
            i.UpdatedAt
        );
    }
}
