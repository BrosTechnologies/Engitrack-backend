using FluentValidation;
using Engitrack.Projects.Application.Projects.Dtos;
using Engitrack.Projects.Domain.Enums;
using System;

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

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must be <= 500 characters");

        RuleFor(x => x.Budget)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Budget.HasValue)
            .WithMessage("Budget must be >= 0");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("EndDate must be >= StartDate");

        RuleFor(x => x.Priority)
            .Must(priority => priority == null || Enum.IsDefined(typeof(Priority), priority.Value))
            .WithMessage("Priority must be a valid value: 0 (LOW), 1 (MEDIUM), or 2 (HIGH)");

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