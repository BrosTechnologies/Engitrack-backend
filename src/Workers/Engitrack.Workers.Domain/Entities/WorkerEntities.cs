using Engitrack.BuildingBlocks.Domain;
using Engitrack.Workers.Domain.Enums;

namespace Engitrack.Workers.Domain.Entities;

public class Worker : AggregateRoot
{
    public string FullName { get; private set; } = string.Empty;
    public string DocumentNumber { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Position { get; private set; } = string.Empty;
    public decimal HourlyRate { get; private set; }

    private readonly List<Assignment> _assignments = new();
    public IReadOnlyCollection<Assignment> Assignments => _assignments.AsReadOnly();

    private Worker() { } // EF Constructor

    public Worker(string fullName, string documentNumber, string phone, string position, decimal hourlyRate)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 120)
            throw new ArgumentException("FullName is required and must be <= 120 characters", nameof(fullName));

        if (string.IsNullOrWhiteSpace(documentNumber) || documentNumber.Length > 32)
            throw new ArgumentException("DocumentNumber is required and must be <= 32 characters", nameof(documentNumber));

        if (hourlyRate < 0)
            throw new ArgumentException("HourlyRate cannot be negative", nameof(hourlyRate));

        FullName = fullName;
        DocumentNumber = documentNumber;
        Phone = phone ?? string.Empty;
        Position = position ?? string.Empty;
        HourlyRate = hourlyRate;
    }

    public void UpdateInfo(string fullName, string phone, string position, decimal hourlyRate)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 120)
            throw new ArgumentException("FullName is required and must be <= 120 characters", nameof(fullName));

        if (hourlyRate < 0)
            throw new ArgumentException("HourlyRate cannot be negative", nameof(hourlyRate));

        FullName = fullName;
        Phone = phone ?? string.Empty;
        Position = position ?? string.Empty;
        HourlyRate = hourlyRate;
        MarkAsUpdated();
    }
}

public class Assignment : Entity
{
    public Guid WorkerId { get; private set; }
    public Guid ProjectId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    // Navigation properties
    public Worker? Worker { get; private set; }

    private Assignment() { } // EF Constructor

    public Assignment(Guid workerId, Guid projectId, DateOnly startDate)
    {
        if (workerId == Guid.Empty)
            throw new ArgumentException("WorkerId is required", nameof(workerId));

        if (projectId == Guid.Empty)
            throw new ArgumentException("ProjectId is required", nameof(projectId));

        WorkerId = workerId;
        ProjectId = projectId;
        StartDate = startDate;
    }

    public void EndAssignment(DateOnly endDate)
    {
        if (endDate < StartDate)
            throw new ArgumentException("EndDate cannot be before StartDate", nameof(endDate));

        EndDate = endDate;
        MarkAsUpdated();
    }
}

public class Attendance : Entity
{
    public Guid WorkerId { get; private set; }
    public Guid ProjectId { get; private set; }
    public DateOnly Day { get; private set; }
    public TimeOnly? CheckIn { get; private set; }
    public TimeOnly? CheckOut { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    // Navigation properties
    public Worker? Worker { get; private set; }

    private Attendance() { } // EF Constructor

    public Attendance(Guid workerId, Guid projectId, DateOnly day, AttendanceStatus status, string? notes = null)
    {
        if (workerId == Guid.Empty)
            throw new ArgumentException("WorkerId is required", nameof(workerId));

        if (projectId == Guid.Empty)
            throw new ArgumentException("ProjectId is required", nameof(projectId));

        WorkerId = workerId;
        ProjectId = projectId;
        Day = day;
        Status = status;
        Notes = notes ?? string.Empty;
    }

    public void SetCheckIn(TimeOnly checkInTime)
    {
        if (Status != AttendanceStatus.PRESENTE)
            throw new InvalidOperationException("Can only check in when status is PRESENTE");

        CheckIn = checkInTime;
        MarkAsUpdated();
    }

    public void SetCheckOut(TimeOnly checkOutTime)
    {
        if (CheckIn == null)
            throw new InvalidOperationException("Cannot check out without checking in first");

        if (checkOutTime <= CheckIn)
            throw new ArgumentException("CheckOut time must be after CheckIn time", nameof(checkOutTime));

        CheckOut = checkOutTime;
        MarkAsUpdated();
    }

    public void UpdateStatus(AttendanceStatus status, string? notes = null)
    {
        Status = status;
        Notes = notes ?? string.Empty;
        MarkAsUpdated();
    }

    public TimeSpan? GetWorkedHours()
    {
        if (CheckIn == null || CheckOut == null)
            return null;

        return CheckOut.Value.ToTimeSpan() - CheckIn.Value.ToTimeSpan();
    }
}