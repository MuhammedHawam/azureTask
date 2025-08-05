using FluentValidation;

namespace AzureProductApi.Application.Products.Commands.CreateProduct;

/// <summary>
/// Validator for CreateProductCommand
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>
    /// Initializes a new instance of the CreateProductCommandValidator class
    /// </summary>
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MaximumLength(200)
            .WithMessage("Product name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Product description is required")
            .MaximumLength(2000)
            .WithMessage("Product description cannot exceed 2000 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Product price must be greater than zero")
            .LessThan(1000000)
            .WithMessage("Product price cannot exceed 1,000,000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO 4217 code")
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be uppercase letters only");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Product category is required")
            .MaximumLength(100)
            .WithMessage("Product category cannot exceed 100 characters");

        RuleFor(x => x.SKU)
            .NotEmpty()
            .WithMessage("Product SKU is required")
            .MaximumLength(50)
            .WithMessage("Product SKU cannot exceed 50 characters")
            .Matches("^[A-Z0-9-_]+$")
            .WithMessage("Product SKU can only contain uppercase letters, numbers, hyphens, and underscores");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stock quantity cannot be negative");

        RuleFor(x => x.Tags)
            .Must(tags => tags.Count <= 10)
            .WithMessage("Cannot have more than 10 tags")
            .Must(tags => tags.All(tag => !string.IsNullOrWhiteSpace(tag)))
            .WithMessage("Tags cannot be empty")
            .Must(tags => tags.All(tag => tag.Length <= 50))
            .WithMessage("Each tag cannot exceed 50 characters");

        RuleFor(x => x.ImageUrl)
            .Must(BeAValidUrl)
            .WithMessage("Image URL must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}