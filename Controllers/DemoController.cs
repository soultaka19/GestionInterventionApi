using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using GestionInterventionApi.DTOs.Demo;
using GestionInterventionApi.Services;

namespace GestionInterventionApi.Controllers;

/// <summary>
/// Point d'entree public de la demonstration. Aucune information n'est demandee
/// au visiteur : ni adresse courriel, ni mot de passe, ni consentement a poser.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class DemoController : ControllerBase
{
    private readonly IDemoService _demoService;

    public DemoController(IDemoService demoService)
    {
        _demoService = demoService;
    }

    /// <summary>
    /// Cree un bac a sable jetable et renvoie de quoi s'y connecter.
    /// </summary>
    [HttpPost("sandbox")]
    [EnableRateLimiting("demo")]
    [ProducesResponseType(typeof(DemoSessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DemoSessionDto>> CreateSandbox(CancellationToken cancellation)
    {
        var session = await _demoService.CreateSandboxAsync(cancellation);

        if (session is null)
        {
            // Le plafond de bacs vivants est atteint. Ce n'est pas une erreur du
            // visiteur : on le dit, avec le delai au bout duquel ca se libere.
            return Problem(
                title: "Démonstration momentanément saturée",
                detail: "Trop de bacs à sable sont ouverts en ce moment. "
                      + "Chacun expire au bout d'une heure ; réessayez dans quelques minutes.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return StatusCode(StatusCodes.Status201Created, session);
    }
}
