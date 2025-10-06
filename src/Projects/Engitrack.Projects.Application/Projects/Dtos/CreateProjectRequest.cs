using Engitrack.Projects.Domain.Enums;

namespace Engitrack.Projects.Application.Projects.Dtos;

public record CreateTaskDto(string Title, DateOnly? DueDate);

public record CreateProjectRequest(
    string Name, 
    DateOnly StartDate, 
    DateOnly? EndDate, 
    decimal? Budget, 
    Guid OwnerUserId, 
    List<CreateTaskDto>? Tasks);

public record ProjectTaskDto(
    Guid TaskId, 
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
    Guid OwnerUserId, 
    IEnumerable<ProjectTaskDto> Tasks);