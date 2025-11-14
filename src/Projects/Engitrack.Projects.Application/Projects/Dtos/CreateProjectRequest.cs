using Engitrack.Projects.Domain.Enums;

namespace Engitrack.Projects.Application.Projects.Dtos;

public record CreateTaskDto(string Title, DateOnly? DueDate);

public record CreateProjectRequest(
    string Name, 
    DateOnly StartDate, 
    DateOnly? EndDate, 
    decimal? Budget, 
    Guid OwnerUserId, 
    Priority? Priority,
    string? Description,
    List<CreateTaskDto>? Tasks);

public record ProjectTaskDto(
    Guid TaskId, 
    Guid ProjectId,
    string Title, 
    string Status, 
    DateOnly? DueDate);

public record ProjectResponse(
    Guid Id, 
    string Name, 
    DateOnly StartDate, 
    DateOnly? EndDate, 
    decimal? Budget, 
    string Status,
    string Priority,
    string? Description,
    Guid OwnerUserId, 
    IEnumerable<ProjectTaskDto> Tasks);

public record CreateTaskRequest(string Title, DateOnly? DueDate);

public record UpdateTaskStatusRequest(string Status);

public record UpdateProjectRequest(string Name, decimal? Budget, DateOnly? EndDate, Priority? Priority, string? Description);

public record UpdatePriorityRequest(Priority Priority);

public record UpdatePriorityStringRequest(string Priority);