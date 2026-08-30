using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionInterventionApi.Data;
using GestionInterventionApi.DTOs.User;
using GestionInterventionApi.Models;
using GestionInterventionApi.Models.Enums;
using GestionInterventionApi.Services;
using System.Security.Cryptography;

namespace GestionInterventionApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TechniciansController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ITenantService _tenantService;

    public TechniciansController(
        ApplicationDbContext context,
        IMapper mapper,
        ITenantService tenantService)
    {
        _context = context;
        _mapper = mapper;
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll([FromQuery] bool activeOnly = true)
    {
        if (!_tenantService.OrganizationId.HasValue)
        {
            return Unauthorized(new { message = "Organisation non identifiée" });
        }

        var query = _context.Users
            .Where(u => u.OrganizationId == _tenantService.OrganizationId.Value)
            .Where(u => u.Role == UserRole.Technicien)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(u => u.IsActive);
        }

        var technicians = await query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync();
        return Ok(_mapper.Map<List<UserDto>>(technicians));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        if (!_tenantService.OrganizationId.HasValue)
        {
            return Unauthorized(new { message = "Organisation non identifiée" });
        }

        var technician = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id
                && u.OrganizationId == _tenantService.OrganizationId.Value
                && u.Role == UserRole.Technicien);

        if (technician == null)
        {
            return NotFound(new { message = "Technicien non trouvé" });
        }

        return Ok(_mapper.Map<UserDto>(technician));
    }

    [HttpPost]
    [Authorize(Policy = "AdministrationComptes")]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto createDto)
    {
        if (!_tenantService.OrganizationId.HasValue)
        {
            return Unauthorized(new { message = "Organisation non identifiée" });
        }

        var emailExists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == createDto.Email);

        if (emailExists)
        {
            return BadRequest(new { message = "Un compte avec cet email existe déjà" });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = createDto.Email,
            PasswordHash = HashPassword(createDto.Password),
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Role = UserRole.Technicien,
            OrganizationId = _tenantService.OrganizationId.Value
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, _mapper.Map<UserDto>(user));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdministrationComptes")]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserDto updateDto)
    {
        if (!_tenantService.OrganizationId.HasValue)
        {
            return Unauthorized(new { message = "Organisation non identifiée" });
        }

        var technician = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id
                && u.OrganizationId == _tenantService.OrganizationId.Value
                && u.Role == UserRole.Technicien);

        if (technician == null)
        {
            return NotFound(new { message = "Technicien non trouvé" });
        }

        technician.FirstName = updateDto.FirstName;
        technician.LastName = updateDto.LastName;
        technician.IsActive = updateDto.IsActive;
        technician.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<UserDto>(technician));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdministrationComptes")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!_tenantService.OrganizationId.HasValue)
        {
            return Unauthorized(new { message = "Organisation non identifiée" });
        }

        var technician = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id
                && u.OrganizationId == _tenantService.OrganizationId.Value
                && u.Role == UserRole.Technicien);

        if (technician == null)
        {
            return NotFound(new { message = "Technicien non trouvé" });
        }

        // Soft delete
        technician.IsActive = false;
        technician.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations: 100000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32
        );

        var hashBytes = new byte[48];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        return Convert.ToBase64String(hashBytes);
    }
}
