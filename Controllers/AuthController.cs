using Microsoft.AspNetCore.Mvc;
using GestionInterventionApi.DTOs.Auth;
using GestionInterventionApi.Services;

namespace GestionInterventionApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        var result = await _authService.LoginAsync(loginDto);

        if (result == null)
        {
            return Unauthorized(new { message = "Email ou mot de passe incorrect" });
        }

        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
    {
        var result = await _authService.RegisterAsync(registerDto);

        if (result == null)
        {
            return BadRequest(new { message = "Un compte avec cet email existe déjà" });
        }

        return CreatedAtAction(nameof(Login), result);
    }
}
