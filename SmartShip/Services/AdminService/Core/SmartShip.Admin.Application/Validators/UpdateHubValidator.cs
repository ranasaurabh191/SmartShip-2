using FluentValidation;
using SmartShip.Admin.Application.DTOs;
namespace SmartShip.Admin.Application.Validators
{
    public class UpdateHubValidator : AbstractValidator<UpdateHubRequest>
    {
        public UpdateHubValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Hub name is required.")
                .MinimumLength(3).WithMessage("Hub name must be at least 3 characters.")
                .MaximumLength(100).WithMessage("Hub name cannot exceed 100 characters.");

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
