namespace GestionInterventionApi.Services;

public class TenantService : ITenantService
{
    private Guid? _organizationId;

    public Guid? OrganizationId => _organizationId;

    public void SetOrganization(Guid organizationId)
    {
        _organizationId = organizationId;
    }
}
