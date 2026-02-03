using FluentValidation;
using GestionInterventionApi.DTOs.Intervention;

namespace GestionInterventionApi.Validators.Intervention;

public class CreateInterventionValidator : AbstractValidator<CreateInterventionDto>
{
    public CreateInterventionValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("Le client est requis");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Le type d'intervention n'est pas valide");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La description ne peut pas dépasser 500 caractères");

        RuleFor(x => x.ScheduledDate)
            .GreaterThanOrEqualTo(DateTime.Today).When(x => x.ScheduledDate.HasValue)
            .WithMessage("La date planifiée ne peut pas être dans le passé");

        RuleFor(x => x.EstimatedDurationMinutes)
            .GreaterThan(0).When(x => x.EstimatedDurationMinutes.HasValue)
            .WithMessage("La durée estimée doit être positive");

        RuleFor(x => x.ScheduledEndTime)
            .GreaterThan(x => x.ScheduledStartTime)
            .When(x => x.ScheduledStartTime.HasValue && x.ScheduledEndTime.HasValue)
            .WithMessage("L'heure de fin doit être après l'heure de début");
    }
}
