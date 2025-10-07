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
    Guid ProjectId,
    DateOnly StartDate);

public record EndAssignmentRequest(
    DateOnly EndDate);

public record CreateAttendanceRequest(
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