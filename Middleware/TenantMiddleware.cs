using System.Security.Claims;
using GestionInterventionApi.Services;

namespace GestionInterventionApi.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var organizationIdClaim = context.User.FindFirst("OrganizationId");
            if (organizationIdClaim != null && Guid.TryParse(organizationIdClaim.Value, out var organizationId))
            {
                tenantService.SetOrganization(organizationId);
            }
        }

        await _next(context);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}
