using FluentValidation;
using SmartShip.Admin.Application.DTOs;

namespace SmartShip.Admin.Application.Validators
{
    public class ReportValidator : AbstractValidator<ReportRequest>
    {
        private readonly string[] _validTypes = { "Operational", "Performance", "SLA", "Delivery" };

        public ReportValidator()
        {
            RuleFor(x => x.ReportType)
                .NotEmpty().WithMessage("Report type is required.")
                .Must(t => _validTypes.Contains(t))
                .WithMessage("Report type must be: Operational, Performance, SLA, or Delivery.");

            RuleFor(x => x.FromDate)
                .NotEmpty().WithMessage("From date is required.")
                .LessThan(x => x.ToDate).WithMessage("From date must be before To date.");

            RuleFor(x => x.ToDate)
                .NotEmpty().WithMessage("To date is required.")
                .GreaterThan(x => x.FromDate).WithMessage("To date must be after From date.");
        }
    }
}
