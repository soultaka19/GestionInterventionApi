using GestionInterventionApi.DTOs.User;
using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.Demo;

/// <summary>
/// Un compte du bac à sable. Les trois rôles sont fournis pour que le visiteur
/// puisse changer de rôle sans quitter la démonstration.
/// </summary>
public record DemoAccountDto(
    string Email,
    UserRole Role,
    string FirstName,
    string LastName
);

/// <summary>
/// Réponse à la création d'un bac à sable : de quoi entrer immédiatement,
/// sans formulaire ni information personnelle.
/// </summary>
public record DemoSessionDto(
    string Token,
    DateTime TokenExpiresAt,
    UserDto User,
    Guid OrganizationId,
    string OrganizationName,
    DateTime SandboxExpiresAt,
    string SharedPassword,
    IReadOnlyList<DemoAccountDto> Accounts
);
