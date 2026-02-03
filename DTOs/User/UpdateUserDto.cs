using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.User;

public record UpdateUserDto(
    string FirstName,
    string LastName,
    UserRole Role,
    bool IsActive
);
