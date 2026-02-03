using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.DTOs.Organization;

public record OrganizationDto(
    Guid Id,
    string Name,
    SubscriptionPlan SubscriptionPlan,
    DateTime CreatedAt
);
