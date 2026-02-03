namespace GestionInterventionApi.DTOs.Auth;

public record RegisterDto(
    string OrganizationName,
    string Email,
    string Password,
    string FirstName,
    string LastName
);
