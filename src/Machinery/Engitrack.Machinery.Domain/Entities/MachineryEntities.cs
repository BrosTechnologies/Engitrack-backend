using Engitrack.BuildingBlocks.Domain;
using Engitrack.Machinery.Domain.Enums;

namespace Engitrack.Machinery.Domain.Entities;

public class Machine : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string SerialNumber { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public MachineStatus Status { get; private set; }
    public DateTime? LastMaintenanceDate { get; private set; }
    public DateTime? NextMaintenanceDate { get; private set; }

    private readonly List<MachineAssignment> _assignments = new();
    public IReadOnlyCollection<MachineAssignment> Assignments => _assignments.AsReadOnly();

    private readonly List<UsageLog> _usageLogs = new();
    public IReadOnlyCollection<UsageLog> UsageLogs => _usageLogs.AsReadOnly();

    private Machine() { } // EF Constructor

    public Machine(string name, string serialNumber, string model)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            throw new ArgumentException("Name is required and must be <= 160 characters", nameof(name));

        if (string.IsNullOrWhiteSpace(serialNumber) || serialNumber.Length > 64)
            throw new ArgumentException("SerialNumber is required and must be <= 64 characters", nameof(serialNumber));

        Name = name;
        SerialNumber = serialNumber;
        Model = model ?? string.Empty;
        Status = MachineStatus.AVAILABLE;
    }

    public void UpdateInfo(string name, string model)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            throw new ArgumentException("Name is required and must be <= 160 characters", nameof(name));

        Name = name;
        Model = model ?? string.Empty;
        MarkAsUpdated();
    }

    public void SetStatus(MachineStatus status)
    {
        Status = status;
        MarkAsUpdated();
    }

    public void PerformMaintenance(DateTime maintenanceDate, DateTime? nextMaintenanceDate = null)
    {
        LastMaintenanceDate = maintenanceDate;
        NextMaintenanceDate = nextMaintenanceDate;
        Status = MachineStatus.AVAILABLE; // Después del mantenimiento queda disponible
        MarkAsUpdated();
    }

    public void StartMaintenance()
    {
        Status = MachineStatus.UNDER_MAINTENANCE;
        MarkAsUpdated();
    }

    public UsageLog LogUsage(Guid projectId, decimal hoursUsed, Guid operatorId, string? notes = null)
    {
        // Regla de negocio: no registrar horas si está en mantenimiento
        if (Status == MachineStatus.UNDER_MAINTENANCE)
            throw new InvalidOperationException("Cannot log usage while machine is under maintenance");

        var usageLog = new UsageLog(Id, projectId, hoursUsed, operatorId, notes);
        _usageLogs.Add(usageLog);
        MarkAsUpdated();
        return usageLog;
    }
}

public class MachineAssignment : Entity
{
    public Guid MachineId { get; private set; }
    public Guid ProjectId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    private MachineAssignment() { } // EF Constructor

    public MachineAssignment(Guid machineId, Guid projectId, DateOnly startDate, string? notes = null)
    {
        if (machineId == Guid.Empty)
            throw new ArgumentException("MachineId is required", nameof(machineId));

        if (projectId == Guid.Empty)
            throw new ArgumentException("ProjectId is required", nameof(projectId));

        MachineId = machineId;
        ProjectId = projectId;
        StartDate = startDate;
        Notes = notes ?? string.Empty;
    }

    public void EndAssignment(DateOnly endDate)
    {
        if (endDate < StartDate)
            throw new ArgumentException("EndDate cannot be before StartDate", nameof(endDate));

        EndDate = endDate;
        MarkAsUpdated();
    }
}

public class UsageLog : Entity
{
    public Guid MachineId { get; private set; }
    public Guid ProjectId { get; private set; }
    public decimal HoursUsed { get; private set; }
    public Guid OperatorId { get; private set; }
    public DateTime LogDate { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    private UsageLog() { } // EF Constructor

    public UsageLog(Guid machineId, Guid projectId, decimal hoursUsed, Guid operatorId, string? notes = null)
    {
        if (machineId == Guid.Empty)
            throw new ArgumentException("MachineId is required", nameof(machineId));

        if (projectId == Guid.Empty)
            throw new ArgumentException("ProjectId is required", nameof(projectId));

        if (operatorId == Guid.Empty)
            throw new ArgumentException("OperatorId is required", nameof(operatorId));

        if (hoursUsed < 0)
            throw new ArgumentException("HoursUsed cannot be negative", nameof(hoursUsed));

        MachineId = machineId;
        ProjectId = projectId;
        HoursUsed = hoursUsed;
        OperatorId = operatorId;
        LogDate = DateTime.UtcNow;
        Notes = notes ?? string.Empty;
    }
}