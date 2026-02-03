using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.Models;

public class User : ITenantEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Technicien;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Multi-tenant
    public Guid OrganizationId { get; set; }

    // Navigation
    public Organization Organization { get; set; } = null!;
}
