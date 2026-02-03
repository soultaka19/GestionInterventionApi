using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.Organization;

public record CreateOrganizationDto(
    string Name,
    SubscriptionPlan SubscriptionPlan = SubscriptionPlan.Free
);
