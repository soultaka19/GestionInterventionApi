using GestionInterventionApi.DTOs.Demo;

namespace GestionInterventionApi.Services;

public interface IDemoService
{
    /// <summary>
    /// Cree une organisation de demonstration jetable, alimentee en donnees
    /// fictives, et renvoie de quoi s'y connecter immediatement.
    /// Renvoie null si le nombre de bacs a sable vivants est deja atteint.
    /// </summary>
    Task<DemoSessionDto?> CreateSandboxAsync(CancellationToken cancellation = default);

    /// <summary>
    /// Supprime les bacs a sable expires. Renvoie le nombre d'organisations
    /// effacees.
    /// </summary>
    Task<int> PurgeExpiredAsync(CancellationToken cancellation = default);
}
