using FluentValidation;
using GestionInterventionApi.DTOs.Equipment;

namespace GestionInterventionApi.Validators.Equipment;

public class CreateEquipmentValidator : AbstractValidator<CreateEquipmentDto>
{
    public CreateEquipmentValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("Le client est requis");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Le type d'équipement n'est pas valide");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("La marque est requise")
            .MaximumLength(50).WithMessage("La marque ne peut pas dépasser 50 caractères");

        RuleFor(x => x.Model)
            .MaximumLength(50).WithMessage("Le modèle ne peut pas dépasser 50 caractères");

        RuleFor(x => x.SerialNumber)
            .MaximumLength(50).WithMessage("Le numéro de série ne peut pas dépasser 50 caractères");

        RuleFor(x => x.InstallationDate)
            .LessThanOrEqualTo(DateTime.Today).When(x => x.InstallationDate.HasValue)
            .WithMessage("La date d'installation ne peut pas être dans le futur");

        RuleFor(x => x.WarrantyEndDate)
            .GreaterThan(x => x.InstallationDate).When(x => x.WarrantyEndDate.HasValue && x.InstallationDate.HasValue)
            .WithMessage("La date de fin de garantie doit être après la date d'installation");
    }
}
