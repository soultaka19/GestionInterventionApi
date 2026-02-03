using GestionInterventionApi.DTOs.User;

namespace GestionInterventionApi.DTOs.Auth;

public record AuthResponseDto(
    string Token,
    DateTime ExpiresAt,
    UserDto User
);
