using FluentValidation;

namespace Engitrack.Api.Contracts.Projects;

public record MachineDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string SerialNumber,
    string Model,
    string Status,
    DateTime? LastMaintenanceDate,
    DateTime? NextMaintenanceDate,
    decimal? HourlyRate
);

public record CreateMachineRequest(
    string Name,
    string SerialNumber,
    string Model,
    string Status,
    decimal? HourlyRate
);

public record UpdateMachineRequest(
    string? Name,
    string? Model,
    string? Status,
    DateTime? LastMaintenanceDate,
    DateTime? NextMaintenanceDate,
    decimal? HourlyRate
);

public class CreateMachineValidator : AbstractValidator<CreateMachineRequest>
{
    public CreateMachineValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(x => x.SerialNumber)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Model)
            .MaximumLength(80);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(BeValidStatus)
            .WithMessage("Status must be one of: OPERATIONAL, UNDER_MAINTENANCE, AVAILABLE");

        RuleFor(x => x.HourlyRate)
            .GreaterThanOrEqualTo(0)
            .When(x => x.HourlyRate.HasValue);
    }

    private static bool BeValidStatus(string status)
    {
        return status is "OPERATIONAL" or "UNDER_MAINTENANCE" or "AVAILABLE";
    }
}

public class UpdateMachineValidator : AbstractValidator<UpdateMachineRequest>
{
    public UpdateMachineValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(160)
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Model)
            .MaximumLength(80)
            .When(x => !string.IsNullOrEmpty(x.Model));

        RuleFor(x => x.Status)
            .Must(BeValidStatus)
            .WithMessage("Status must be one of: OPERATIONAL, UNDER_MAINTENANCE, AVAILABLE")
            .When(x => !string.IsNullOrEmpty(x.Status));

        RuleFor(x => x.HourlyRate)
            .GreaterThanOrEqualTo(0)
            .When(x => x.HourlyRate.HasValue);
    }

    private static bool BeValidStatus(string status)
    {
        return status is "OPERATIONAL" or "UNDER_MAINTENANCE" or "AVAILABLE";
    }
}