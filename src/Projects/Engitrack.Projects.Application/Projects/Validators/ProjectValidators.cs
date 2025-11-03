using FluentValidation;
using Engitrack.Projects.Application.Projects.Dtos;
using Engitrack.Projects.Domain.Enums;
using System;

namespace Engitrack.Projects.Application.Projects.Validators;

public class CreateTaskDtoValidator : AbstractValidator<CreateTaskDto>
{
    public CreateTaskDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(160);
    }
}

public class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(status => status is "PENDING" or "IN_PROGRESS" or "DONE")
            .WithMessage("Status must be PENDING, IN_PROGRESS, or DONE");
    }
}

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(160);
    }
}

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(x => x.Budget)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Budget.HasValue);

        RuleFor(x => x.Priority)
            .Must(priority => priority == null || Enum.IsDefined(typeof(Priority), priority.Value))
            .WithMessage("Priority must be a valid value: 0 (LOW), 1 (MEDIUM), or 2 (HIGH)");
    }
}

public class UpdatePriorityRequestValidator : AbstractValidator<UpdatePriorityRequest>
{
    public UpdatePriorityRequestValidator()
    {
        RuleFor(x => x.Priority)
            .Must(priority => Enum.IsDefined(typeof(Priority), priority))
            .WithMessage("Priority must be a valid value: 0 (LOW), 1 (MEDIUM), or 2 (HIGH)");
    }
}

public class UpdatePriorityStringRequestValidator : AbstractValidator<UpdatePriorityStringRequest>
{
    public UpdatePriorityStringRequestValidator()
    {
        RuleFor(x => x.Priority)
            .NotEmpty()
            .Must(priority => priority.ToUpper() is "LOW" or "MEDIUM" or "HIGH")
            .WithMessage("Priority must be: LOW, MEDIUM, or HIGH");
    }
}