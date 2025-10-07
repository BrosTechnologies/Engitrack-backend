using FluentValidation;
using Engitrack.Inventory.Application.Dtos;

namespace Engitrack.Inventory.Application.Validators;

public class RegisterTransactionRequestValidator : AbstractValidator<RegisterTransactionRequest>
{
    public RegisterTransactionRequestValidator()
    {
        RuleFor(x => x.MaterialId)
            .NotEmpty().WithMessage("MaterialId is required");

        RuleFor(x => x.TxType)
            .NotEmpty().WithMessage("TxType is required")
            .Must(x => x == "ENTRY" || x == "USAGE" || x == "ADJUSTMENT")
            .WithMessage("TxType must be ENTRY, USAGE, or ADJUSTMENT");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.Notes)
            .MaximumLength(400).WithMessage("Notes cannot exceed 400 characters");
    }
}