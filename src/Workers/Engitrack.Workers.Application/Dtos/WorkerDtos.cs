namespace Engitrack.Workers.Application.Dtos;

public record CreateWorkerRequest(
    string FullName,
    string DocumentNumber,
    string Phone,
    string Position,
    decimal HourlyRate,
    Guid ProjectId);

public record UpdateWorkerRequest(
    string FullName,
    string Phone,
    string Position,
    decimal HourlyRate);

public record CreateAssignmentRequest(
    Guid WorkerId,
    Guid ProjectId,
    DateOnly StartDate);

public record EndAssignmentRequest(
    DateOnly EndDate);

public record CreateAttendanceRequest(
    Guid WorkerId,
    Guid ProjectId,
    DateOnly Day,
    string Status,
    string? Notes = null);

public record UpdateAttendanceRequest(
    TimeOnly? CheckIn,
    TimeOnly? CheckOut,
    string Status,
    string? Notes = null);

public record WorkerResponse(
    Guid Id,
    string FullName,
    string DocumentNumber,
    string Phone,
    string Position,
    decimal HourlyRate,
    IEnumerable<AssignmentDto> Assignments);

public record AssignmentDto(
    Guid Id,
    Guid WorkerId,
    Guid ProjectId,
    DateOnly StartDate,
    DateOnly? EndDate);

public record AttendanceDto(
    Guid Id,
    Guid WorkerId,
    Guid ProjectId,
    DateOnly Day,
    TimeOnly? CheckIn,
    TimeOnly? CheckOut,
    string Status,
    string Notes);

public record AssignmentResponse(
    Guid Id,
    Guid WorkerId,
    Guid ProjectId,
    string ProjectName,
    DateOnly StartDate,
    DateOnly? EndDate);

public record ProjectWorkerResponse(
    Guid WorkerId,
    string FullName,
    string DocumentNumber,
    string Phone,
    string Position,
    decimal HourlyRate,
    Guid AssignmentId,
    DateOnly StartDate,
    DateOnly? EndDate);

public record AssignWorkerRequest(
    Guid WorkerId,
    DateOnly StartDate,
    DateOnly? EndDate = null);

public record WorkerAssignmentResponse(
    Guid AssignmentId,
    Guid ProjectId,
    string ProjectName,
    DateOnly StartDate,
    DateOnly? EndDate);

public record AttendanceResponse(
    Guid Id,
    Guid WorkerId,
    string WorkerName,
    Guid ProjectId,
    string ProjectName,
    DateOnly Day,
    TimeOnly? CheckIn,
    TimeOnly? CheckOut,
    string Status,
    string Notes);