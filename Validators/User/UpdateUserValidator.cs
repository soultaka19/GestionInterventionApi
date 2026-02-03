using FluentValidation;
using GestionInterventionApi.DTOs.User;

namespace GestionInterventionApi.Validators.User;

public class UpdateUserValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Le prénom est requis")
            .MaximumLength(50).WithMessage("Le prénom ne peut pas dépasser 50 caractères");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Le nom est requis")
            .MaximumLength(50).WithMessage("Le nom ne peut pas dépasser 50 caractères");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Le rôle n'est pas valide");
    }
}
