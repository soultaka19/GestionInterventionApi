using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.User;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    bool IsActive,
    Guid OrganizationId,
    DateTime CreatedAt
);
