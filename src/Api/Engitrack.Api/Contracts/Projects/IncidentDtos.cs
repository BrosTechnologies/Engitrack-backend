using FluentValidation;

namespace Engitrack.Api.Contracts.Projects;

public record IncidentDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Description,
    string Severity,
    string Status,
    Guid ReportedBy,
    DateTime ReportedAt,
    Guid? AssignedTo,
    DateTime? ResolvedAt
);

public record CreateIncidentRequest(
    string Title,
    string Description,
    string Severity,
    Guid ReportedBy
);

public record UpdateIncidentRequest(
    string? Title,
    string? Description,
    string? Severity,
    Guid? AssignedTo,
    string? Status
);

public class CreateIncidentValidator : AbstractValidator<CreateIncidentRequest>
{
    public CreateIncidentValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.Severity)
            .NotEmpty()
            .Must(BeValidSeverity)
            .WithMessage("Severity must be one of: LOW, MEDIUM, HIGH, CRITICAL");

        RuleFor(x => x.ReportedBy)
            .NotEmpty();
    }

    private static bool BeValidSeverity(string severity)
    {
        return severity is "LOW" or "MEDIUM" or "HIGH" or "CRITICAL";
    }
}

public class UpdateIncidentValidator : AbstractValidator<UpdateIncidentRequest>
{
    public UpdateIncidentValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(160)
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Severity)
            .Must(BeValidSeverity)
            .WithMessage("Severity must be one of: LOW, MEDIUM, HIGH, CRITICAL")
            .When(x => !string.IsNullOrEmpty(x.Severity));

        RuleFor(x => x.Status)
            .Must(BeValidStatus)
            .WithMessage("Status must be one of: OPEN, IN_PROGRESS, RESOLVED, CLOSED")
            .When(x => !string.IsNullOrEmpty(x.Status));
    }

    private static bool BeValidSeverity(string severity)
    {
        return severity is "LOW" or "MEDIUM" or "HIGH" or "CRITICAL";
    }

    private static bool BeValidStatus(string status)
    {
        return status is "OPEN" or "IN_PROGRESS" or "RESOLVED" or "CLOSED";
    }
}