using FluentValidation;
using Engitrack.Inventory.Application.Dtos;
using Engitrack.Inventory.Domain.Materials;

namespace Engitrack.Inventory.Application.Validators;

public class CreateMaterialRequestValidator : AbstractValidator<CreateMaterialRequest>
{
    public CreateMaterialRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("ProjectId is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(160)
            .WithMessage("Name is required and must be <= 160 characters");

        RuleFor(x => x.Unit)
            .NotEmpty()
            .MaximumLength(32)
            .WithMessage("Unit is required and must be <= 32 characters");

        RuleFor(x => x.MinNivel)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MinNivel must be >= 0");
    }
}

public class UpdateMaterialRequestValidator : AbstractValidator<UpdateMaterialRequest>
{
    public UpdateMaterialRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(160)
            .When(x => !string.IsNullOrEmpty(x.Name))
            .WithMessage("Name must be <= 160 characters");

        RuleFor(x => x.Unit)
            .MaximumLength(32)
            .When(x => !string.IsNullOrEmpty(x.Unit))
            .WithMessage("Unit must be <= 32 characters");

        RuleFor(x => x.MinNivel)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinNivel.HasValue)
            .WithMessage("MinNivel must be >= 0");
    }
}

public class RegisterTransactionRequestValidator : AbstractValidator<RegisterTransactionRequest>
{
    public RegisterTransactionRequestValidator()
    {
        RuleFor(x => x.TxType)
            .NotEmpty()
            .Must(BeValidTxType)
            .WithMessage("TxType must be one of: ENTRY, USAGE, ADJUSTMENT");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be > 0");

        RuleFor(x => x.Notes)
            .MaximumLength(400)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage("Notes must be <= 400 characters");
    }

    private static bool BeValidTxType(string txType)
    {
        return txType is "ENTRY" or "USAGE" or "ADJUSTMENT";
    }
}

public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(160)
            .WithMessage("Name is required and must be <= 160 characters");

        RuleFor(x => x.Ruc)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Ruc))
            .WithMessage("Ruc must be <= 20 characters");

        RuleFor(x => x.Phone)
            .MaximumLength(32)
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must be <= 32 characters");

        RuleFor(x => x.Email)
            .MaximumLength(160)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email must be valid and <= 160 characters");
    }
}

public class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(160)
            .When(x => !string.IsNullOrEmpty(x.Name))
            .WithMessage("Name must be <= 160 characters");

        RuleFor(x => x.Ruc)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Ruc))
            .WithMessage("Ruc must be <= 20 characters");

        RuleFor(x => x.Phone)
            .MaximumLength(32)
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must be <= 32 characters");

        RuleFor(x => x.Email)
            .MaximumLength(160)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email must be valid and <= 160 characters");
    }
}