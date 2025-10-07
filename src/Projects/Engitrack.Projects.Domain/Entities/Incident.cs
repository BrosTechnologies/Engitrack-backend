using Engitrack.BuildingBlocks.Domain;

namespace Engitrack.Projects.Domain.Entities;

public enum IncidentSeverity
{
    LOW,
    MEDIUM,
    HIGH,
    CRITICAL
}

public enum IncidentStatus
{
    OPEN,
    IN_PROGRESS,
    RESOLVED,
    CLOSED
}

public class Incident : Entity
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; }
    public Guid ReportedBy { get; set; }
    public DateTime ReportedAt { get; set; }
    public Guid? AssignedTo { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    // Navigation property
    public Project Project { get; set; } = null!;
}