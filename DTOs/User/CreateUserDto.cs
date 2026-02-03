using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.User;

public record CreateUserDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role = UserRole.Technicien
);
