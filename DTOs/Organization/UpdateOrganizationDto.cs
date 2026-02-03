using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.Organization;

public record UpdateOrganizationDto(
    string Name,
    SubscriptionPlan SubscriptionPlan
);
