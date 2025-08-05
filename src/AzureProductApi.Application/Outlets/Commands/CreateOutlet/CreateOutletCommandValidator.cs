using FluentValidation;

namespace AzureProductApi.Application.Outlets.Commands.CreateOutlet;

/// <summary>
/// Validator for CreateOutletCommand
/// </summary>
public class CreateOutletCommandValidator : AbstractValidator<CreateOutletCommand>
{
    /// <summary>
    /// Initializes a new instance of the CreateOutletCommandValidator class
    /// </summary>
    public CreateOutletCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Outlet name is required")
            .MaximumLength(200)
            .WithMessage("Outlet name cannot exceed 200 characters");

        RuleFor(x => x.Tier)
            .NotEmpty()
            .WithMessage("Outlet tier is required")
            .MaximumLength(50)
            .WithMessage("Outlet tier cannot exceed 50 characters");

        RuleFor(x => x.Rank)
            .GreaterThan(0)
            .WithMessage("Outlet rank must be greater than zero");

        RuleFor(x => x.ChainType)
            .IsInEnum()
            .WithMessage("Invalid chain type");

        RuleFor(x => x.Sales)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Sales amount cannot be negative");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO 4217 code")
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be uppercase letters only");

        RuleFor(x => x.VolumeSoldKg)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Volume sold cannot be negative");

        RuleFor(x => x.VolumeTargetKg)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Volume target cannot be negative");

        RuleFor(x => x.Address)
            .NotNull()
            .WithMessage("Address is required");

        RuleFor(x => x.Address.Street)
            .NotEmpty()
            .WithMessage("Street address is required")
            .MaximumLength(200)
            .WithMessage("Street address cannot exceed 200 characters")
            .When(x => x.Address != null);

        RuleFor(x => x.Address.City)
            .NotEmpty()
            .WithMessage("City is required")
            .MaximumLength(100)
            .WithMessage("City cannot exceed 100 characters")
            .When(x => x.Address != null);

        RuleFor(x => x.Address.State)
            .NotEmpty()
            .WithMessage("State is required")
            .MaximumLength(100)
            .WithMessage("State cannot exceed 100 characters")
            .When(x => x.Address != null);

        RuleFor(x => x.Address.PostalCode)
            .NotEmpty()
            .WithMessage("Postal code is required")
            .MaximumLength(20)
            .WithMessage("Postal code cannot exceed 20 characters")
            .When(x => x.Address != null);

        RuleFor(x => x.Address.Country)
            .NotEmpty()
            .WithMessage("Country is required")
            .MaximumLength(100)
            .WithMessage("Country cannot exceed 100 characters")
            .When(x => x.Address != null);

        RuleFor(x => x.LastVisitDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Last visit date cannot be in the future")
            .When(x => x.LastVisitDate.HasValue);

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}