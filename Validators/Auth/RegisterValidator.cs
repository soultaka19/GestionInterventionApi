using FluentValidation;
using GestionInterventionApi.DTOs.Auth;

namespace GestionInterventionApi.Validators.Auth;

public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithMessage("Le nom de l'organisation est requis")
            .MinimumLength(2).WithMessage("Le nom doit contenir au moins 2 caractères")
            .MaximumLength(100).WithMessage("Le nom ne peut pas dépasser 100 caractères");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est requis")
            .EmailAddress().WithMessage("L'email n'est pas valide");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Le mot de passe est requis")
            .MinimumLength(8).WithMessage("Le mot de passe doit contenir au moins 8 caractères")
            .Matches("[A-Z]").WithMessage("Le mot de passe doit contenir au moins une majuscule")
            .Matches("[a-z]").WithMessage("Le mot de passe doit contenir au moins une minuscule")
            .Matches("[0-9]").WithMessage("Le mot de passe doit contenir au moins un chiffre");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Le prénom est requis")
            .MaximumLength(50).WithMessage("Le prénom ne peut pas dépasser 50 caractères");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Le nom est requis")
            .MaximumLength(50).WithMessage("Le nom ne peut pas dépasser 50 caractères");
    }
}
