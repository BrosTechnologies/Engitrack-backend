using FluentValidation;
using Engitrack.Projects.Application.Projects.Dtos;
using Engitrack.Projects.Domain.Enums;

namespace Engitrack.Projects.Application.Projects.Validators;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(160)
            .WithMessage("Name must be <= 160 characters");

        RuleFor(x => x.Budget)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Budget.HasValue)
            .WithMessage("Budget must be >= 0");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("EndDate must be >= StartDate");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .When(x => x.Priority.HasValue)
            .WithMessage("Priority must be a valid value (LOW, MEDIUM, HIGH)");

        RuleForEach(x => x.Tasks)
            .ChildRules(task =>
            {
                task.RuleFor(t => t.Title)
                    .NotEmpty()
                    .WithMessage("Task title is required")
                    .MaximumLength(160)
                    .WithMessage("Task title must be <= 160 characters");
            })
            .When(x => x.Tasks != null);
    }
}