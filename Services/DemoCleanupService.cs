namespace GestionInterventionApi.Services;

/// <summary>
/// Efface les bacs a sable expires. Sans lui, la promesse « jetable » serait
/// fausse : les donnees d'un visiteur resteraient en base indefiniment.
/// </summary>
public class DemoCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DemoCleanupService> _logger;

    /// <summary>Periode entre deux purges (Demo__CleanupIntervalSeconds).</summary>
    private readonly TimeSpan _intervalle;

    public DemoCleanupService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<DemoCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _intervalle = TimeSpan.FromSeconds(
            configuration.GetValue("Demo:CleanupIntervalSeconds", 300));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Un premier passage au demarrage : si l'API a ete arretee plusieurs
        // heures, des bacs expires attendent deja.
        using var minuterie = new PeriodicTimer(_intervalle);

        do
        {
            try
            {
                // IDemoService est enregistre en Scoped (il depend du DbContext) :
                // un service heberge est un singleton, il lui faut sa propre portee.
                using var portee = _scopeFactory.CreateScope();
                var demoService = portee.ServiceProvider.GetRequiredService<IDemoService>();
                await demoService.PurgeExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Une purge ratee ne doit pas tuer le service : elle sera
                // retentee au prochain passage.
                _logger.LogError(exception, "Echec de la purge des bacs a sable");
            }
        }
        while (await AttendreProchainPassage(minuterie, stoppingToken));
    }

    private static async Task<bool> AttendreProchainPassage(
        PeriodicTimer minuterie, CancellationToken stoppingToken)
    {
        try
        {
            return await minuterie.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
