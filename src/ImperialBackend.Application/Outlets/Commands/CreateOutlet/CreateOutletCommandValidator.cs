using FluentValidation;

namespace ImperialBackend.Application.Outlets.Commands.CreateOutlet;

public class CreateOutletCommandValidator : AbstractValidator<CreateOutletCommand>
{
    public CreateOutletCommandValidator()
    {
        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(1900)
            .WithMessage("Year must be >= 1900");

        RuleFor(x => x.Week)
            .InclusiveBetween(1, 53)
            .WithMessage("Week must be between 1 and 53");

        RuleFor(x => x.TotalOuterQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("TotalOuterQuantity cannot be negative");

        RuleFor(x => x.CountOuterQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("CountOuterQuantity cannot be negative");

        RuleFor(x => x.TotalSales6w)
            .GreaterThanOrEqualTo(0)
            .WithMessage("TotalSales6w cannot be negative");

        RuleFor(x => x.HealthStatus)
            .NotEmpty()
            .WithMessage("HealthStatus is required")
            .MaximumLength(50);

        RuleFor(x => x.StoreRank)
            .GreaterThanOrEqualTo(0)
            .WithMessage("StoreRank cannot be negative");

        RuleFor(x => x.OutletName)
            .NotEmpty()
            .WithMessage("OutletName is required")
            .MaximumLength(200);

        RuleFor(x => x.OutletIdentifier)
            .NotEmpty()
            .WithMessage("OutletIdentifier is required")
            .MaximumLength(100);

        RuleFor(x => x.AddressLine1)
            .NotEmpty()
            .WithMessage("AddressLine1 is required")
            .MaximumLength(200);

        RuleFor(x => x.State)
            .NotEmpty()
            .WithMessage("State is required")
            .MaximumLength(50);

        RuleFor(x => x.County)
            .NotEmpty()
            .WithMessage("County is required")
            .MaximumLength(100);

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}