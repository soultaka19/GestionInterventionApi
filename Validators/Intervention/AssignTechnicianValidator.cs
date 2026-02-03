using FluentValidation;
using GestionInterventionApi.DTOs.Intervention;

namespace GestionInterventionApi.Validators.Intervention;

public class AssignTechnicianValidator : AbstractValidator<AssignTechnicianDto>
{
    public AssignTechnicianValidator()
    {
        RuleFor(x => x.TechnicianId)
            .NotEmpty().WithMessage("Le technicien est requis");

        RuleFor(x => x.ScheduledEndTime)
            .GreaterThan(x => x.ScheduledStartTime)
            .When(x => x.ScheduledStartTime.HasValue && x.ScheduledEndTime.HasValue)
            .WithMessage("L'heure de fin doit être après l'heure de début");
    }
}
