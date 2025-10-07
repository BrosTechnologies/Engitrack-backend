using FluentValidation;
using Engitrack.Workers.Application.Dtos;

namespace Engitrack.Workers.Application.Validators;

public class CreateWorkerRequestValidator : AbstractValidator<CreateWorkerRequest>
{
    public CreateWorkerRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName is required")
            .MaximumLength(120).WithMessage("FullName cannot exceed 120 characters");

        RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage("DocumentNumber is required")
            .MaximumLength(32).WithMessage("DocumentNumber cannot exceed 32 characters");

        RuleFor(x => x.HourlyRate)
            .GreaterThanOrEqualTo(0).WithMessage("HourlyRate cannot be negative");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required");
    }
}

public class UpdateWorkerRequestValidator : AbstractValidator<UpdateWorkerRequest>
{
    public UpdateWorkerRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName is required")
            .MaximumLength(120).WithMessage("FullName cannot exceed 120 characters");

        RuleFor(x => x.HourlyRate)
            .GreaterThanOrEqualTo(0).WithMessage("HourlyRate cannot be negative");
    }
}

public class CreateAssignmentRequestValidator : AbstractValidator<CreateAssignmentRequest>
{
    public CreateAssignmentRequestValidator()
    {
        RuleFor(x => x.WorkerId)
            .NotEmpty().WithMessage("WorkerId is required");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("StartDate is required");
    }
}

public class CreateAttendanceRequestValidator : AbstractValidator<CreateAttendanceRequest>
{
    public CreateAttendanceRequestValidator()
    {
        RuleFor(x => x.WorkerId)
            .NotEmpty().WithMessage("WorkerId is required");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required");

        RuleFor(x => x.Day)
            .NotEmpty().WithMessage("Day is required");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(x => x == "PRESENTE" || x == "AUSENTE" || x == "JUSTIFICADO")
            .WithMessage("Status must be PRESENTE, AUSENTE, or JUSTIFICADO");
    }
}