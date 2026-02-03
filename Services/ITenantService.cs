namespace GestionInterventionApi.Services;

public interface ITenantService
{
    Guid? OrganizationId { get; }
    void SetOrganization(Guid organizationId);
}
