using FluentValidation;
using GestionInterventionApi.DTOs.Client;

namespace GestionInterventionApi.Validators.Client;

public class CreateClientValidator : AbstractValidator<CreateClientDto>
{
    public CreateClientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom du client est requis")
            .MaximumLength(100).WithMessage("Le nom ne peut pas dépasser 100 caractères");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("L'adresse est requise")
            .MaximumLength(200).WithMessage("L'adresse ne peut pas dépasser 200 caractères");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("La ville ne peut pas dépasser 100 caractères");

        RuleFor(x => x.PostalCode)
            .MaximumLength(10).WithMessage("Le code postal ne peut pas dépasser 10 caractères");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Le téléphone ne peut pas dépasser 20 caractères");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("L'email n'est pas valide");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue)
            .WithMessage("La latitude doit être entre -90 et 90");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue)
            .WithMessage("La longitude doit être entre -180 et 180");
    }
}
