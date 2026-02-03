using FluentValidation;
using GestionInterventionApi.DTOs.Organization;

namespace GestionInterventionApi.Validators.Organization;

public class UpdateOrganizationValidator : AbstractValidator<UpdateOrganizationDto>
{
    public UpdateOrganizationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom de l'organisation est requis")
            .MinimumLength(2).WithMessage("Le nom doit contenir au moins 2 caractères")
            .MaximumLength(100).WithMessage("Le nom ne peut pas dépasser 100 caractères");

        RuleFor(x => x.SubscriptionPlan)
            .IsInEnum().WithMessage("Le plan d'abonnement n'est pas valide");
    }
}
