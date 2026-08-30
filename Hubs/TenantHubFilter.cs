using Microsoft.AspNetCore.SignalR;
using GestionInterventionApi.Services;

namespace GestionInterventionApi.Hubs;

/// <summary>
/// Renseigne l'organisation courante avant chaque invocation de methode SignalR.
///
/// Pourquoi ce filtre existe : <c>TenantMiddleware</c> ne s'execute que sur le
/// pipeline HTTP, donc uniquement sur la requete de negociation. Chaque appel de
/// methode de hub obtient ensuite un scope de dependances NEUF, dans lequel
/// <see cref="ITenantService"/> est vierge — <c>OrganizationId</c> y valait
/// <c>null</c>. Combine a l'ancien filtre fail-open du DbContext, cela ouvrait
/// les donnees de toutes les organisations aux methodes de hub.
///
/// Le filtre lit la revendication <c>OrganizationId</c> du jeton, la meme source
/// que le middleware HTTP : aucune confiance n'est accordee aux arguments client.
/// </summary>
public class TenantHubFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        AppliquerTenant(invocationContext.ServiceProvider, invocationContext.Context);
        return await next(invocationContext);
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        AppliquerTenant(context.ServiceProvider, context.Context);
        await next(context);
    }

    public async Task OnDisconnectedAsync(
        HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        AppliquerTenant(context.ServiceProvider, context.Context);
        await next(context, exception);
    }

    private static void AppliquerTenant(IServiceProvider services, HubCallerContext context)
    {
        var revendication = context.User?.FindFirst("OrganizationId")?.Value;
        if (Guid.TryParse(revendication, out var organizationId))
        {
            services.GetRequiredService<ITenantService>().SetOrganization(organizationId);
        }
    }
}
