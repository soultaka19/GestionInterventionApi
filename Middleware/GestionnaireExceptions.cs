using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GestionInterventionApi.Middleware;

/// <summary>
/// Gestionnaire global d'exceptions (B-8).
///
/// Avant : aucun <c>UseExceptionHandler</c>, aucun <c>AddProblemDetails</c>.
/// Hors developpement, toute exception non geree produisait un <b>500 au corps
/// vide</b> — le client ne savait ni ce qui avait echoue, ni si reessayer avait
/// un sens. Trois cas se produisaient reellement :
///
///   - <c>DELETE /api/clients/{id}</c> sur un client ayant des interventions :
///     la cle etrangere est en <c>Restrict</c>, EF leve une DbUpdateException.
///     C'est un <b>conflit metier</b> (409), pas une panne serveur.
///   - <c>PUT /api/interventions/{id}</c> avec un TechnicianId inexistant :
///     violation de cle etrangere, meme traitement.
///   - hash de mot de passe corrompu : desormais intercepte en amont (B-13).
///
/// Les reponses suivent RFC 7807 (ProblemDetails). Le detail interne n'est
/// jamais renvoye : il part dans les journaux, avec l'identifiant de trace que
/// le client recoit — de quoi relier un rapport d'utilisateur a une ligne de log
/// sans rien exposer.
/// </summary>
public sealed class GestionnaireExceptions : IExceptionHandler
{
    private readonly ILogger<GestionnaireExceptions> _logger;

    public GestionnaireExceptions(ILogger<GestionnaireExceptions> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexte, Exception exception, CancellationToken cancellationToken)
    {
        // Une annulation cote client n'est pas une erreur : le client est parti,
        // il n'y a personne pour lire la reponse et rien a signaler.
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        var (statut, titre, detail) = Classer(exception);

        if (statut >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Exception non geree sur {Methode} {Chemin}",
                contexte.Request.Method, contexte.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Requete refusee ({Statut}) sur {Methode} {Chemin}",
                statut, contexte.Request.Method, contexte.Request.Path);
        }

        var probleme = new ProblemDetails
        {
            Status = statut,
            Title = titre,
            Detail = detail,
            Instance = $"{contexte.Request.Method} {contexte.Request.Path}",
        };
        // Permet de retrouver la ligne de journal correspondante sans divulguer
        // quoi que ce soit de l'erreur elle-meme.
        probleme.Extensions["traceId"] = contexte.TraceIdentifier;

        contexte.Response.StatusCode = statut;
        await contexte.Response.WriteAsJsonAsync(probleme, cancellationToken);
        return true;
    }

    private static (int Statut, string Titre, string Detail) Classer(Exception exception) =>
        exception switch
        {
            DbUpdateException db when EstViolationCleEtrangere(db) => (
                StatusCodes.Status409Conflict,
                "Conflit de donnees",
                "L'operation reference un enregistrement inexistant, ou supprime un "
                + "enregistrement encore utilise ailleurs."),

            DbUpdateException db when EstViolationUnicite(db) => (
                StatusCodes.Status409Conflict,
                "Conflit de donnees",
                "Un enregistrement equivalent existe deja."),

            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Conflit de mise a jour",
                "L'enregistrement a ete modifie entre-temps. Rechargez-le et reessayez."),

            // Les appels sortants (Google) ont un delai d'attente de 10 s (B-10).
            TaskCanceledException or TimeoutException or HttpRequestException => (
                StatusCodes.Status503ServiceUnavailable,
                "Service externe indisponible",
                "Un service tiers n'a pas repondu a temps. Reessayez plus tard."),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Erreur interne",
                "Une erreur inattendue s'est produite."),
        };

    // PostgreSQL : 23503 = foreign_key_violation, 23505 = unique_violation.
    private static bool EstViolationCleEtrangere(DbUpdateException e) =>
        e.InnerException is PostgresException { SqlState: "23503" };

    private static bool EstViolationUnicite(DbUpdateException e) =>
        e.InnerException is PostgresException { SqlState: "23505" };
}
