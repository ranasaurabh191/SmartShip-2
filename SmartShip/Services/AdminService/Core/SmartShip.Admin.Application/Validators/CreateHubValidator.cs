using FluentValidation;
using SmartShip.Admin.Application.DTOs;
namespace SmartShip.Admin.Application.Validators
{
    public class CreateHubValidator : AbstractValidator<CreateHubRequest>
    {
        public CreateHubValidator()
        {
            RuleFor(X => X.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MinimumLength(3).WithMessage("Name must be at least 3 characters.")
                .MaximumLength(100).WithMessage("Name must be less than 100 characters.");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required.")
                .Matches(@"^[a-zA-Z\s]+$").WithMessage("City can only contain letters.");

            RuleFor(x => x.State)
                .NotEmpty().WithMessage("State is required.")
                .Matches(@"^[a-zA-Z\s]+$").WithMessage("State can only contain letters.");

            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Country is required.");

            RuleFor(x => x.ContactPhone)
                .NotEmpty().WithMessage("Contact phone is required.")
                .Matches(@"^\d{10}$").WithMessage("Phone must be exactly 10 digits.");
        }
    }
}
