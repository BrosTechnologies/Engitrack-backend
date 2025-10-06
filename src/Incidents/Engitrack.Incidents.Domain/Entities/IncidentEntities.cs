using Engitrack.BuildingBlocks.Domain;
using Engitrack.Incidents.Domain.Enums;

namespace Engitrack.Incidents.Domain.Entities;

public class Incident : AggregateRoot
{
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public IncidentSeverity Severity { get; private set; }
    public IncidentStatus Status { get; private set; }
    public Guid ReportedBy { get; private set; }
    public DateTime ReportedAt { get; private set; }
    public Guid? AssignedTo { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private readonly List<Attachment> _attachments = new();
    public IReadOnlyCollection<Attachment> Attachments => _attachments.AsReadOnly();

    private Incident() { } // EF Constructor

    public Incident(Guid projectId, string title, string description, IncidentSeverity severity, Guid reportedBy)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException("ProjectId is required", nameof(projectId));

        if (string.IsNullOrWhiteSpace(title) || title.Length > 160)
            throw new ArgumentException("Title is required and must be <= 160 characters", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        if (reportedBy == Guid.Empty)
            throw new ArgumentException("ReportedBy is required", nameof(reportedBy));

        ProjectId = projectId;
        Title = title;
        Description = description;
        Severity = severity;
        Status = IncidentStatus.OPEN;
        ReportedBy = reportedBy;
        ReportedAt = DateTime.UtcNow;

        // Regla de negocio: si es HIGH o CRITICAL, preparar IntegrationEvent
        if (severity == IncidentSeverity.HIGH || severity == IncidentSeverity.CRITICAL)
        {
            RaiseDomainEvent(new HighSeverityIncidentReported(Id, ProjectId, severity, title));
        }
    }

    public void AssignTo(Guid userId)
    {
        if (Status == IncidentStatus.CLOSED || Status == IncidentStatus.RESOLVED)
            throw new InvalidOperationException("Cannot assign a resolved or closed incident");

        AssignedTo = userId;
        if (Status == IncidentStatus.OPEN)
            Status = IncidentStatus.IN_PROGRESS;
        
        MarkAsUpdated();
    }

    public void Resolve(string? resolutionNotes = null)
    {
        if (Status == IncidentStatus.CLOSED)
            throw new InvalidOperationException("Cannot resolve a closed incident");

        Status = IncidentStatus.RESOLVED;
        ResolvedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Close()
    {
        if (Status != IncidentStatus.RESOLVED)
            throw new InvalidOperationException("Can only close resolved incidents");

        Status = IncidentStatus.CLOSED;
        MarkAsUpdated();
    }

    public void Reopen()
    {
        if (Status == IncidentStatus.OPEN || Status == IncidentStatus.IN_PROGRESS)
            throw new InvalidOperationException("Incident is already open");

        Status = AssignedTo.HasValue ? IncidentStatus.IN_PROGRESS : IncidentStatus.OPEN;
        ResolvedAt = null;
        MarkAsUpdated();
    }

    public Attachment AddAttachment(string fileName, string filePath, long fileSize)
    {
        var attachment = new Attachment(Id, fileName, filePath, fileSize);
        _attachments.Add(attachment);
        MarkAsUpdated();
        return attachment;
    }
}

public class Attachment : Entity
{
    public Guid IncidentId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private Attachment() { } // EF Constructor

    public Attachment(Guid incidentId, string fileName, string filePath, long fileSize)
    {
        if (incidentId == Guid.Empty)
            throw new ArgumentException("IncidentId is required", nameof(incidentId));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName is required", nameof(fileName));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("FilePath is required", nameof(filePath));

        if (fileSize <= 0)
            throw new ArgumentException("FileSize must be greater than 0", nameof(fileSize));

        IncidentId = incidentId;
        FileName = fileName;
        FilePath = filePath;
        FileSize = fileSize;
        UploadedAt = DateTime.UtcNow;
    }
}

// Domain Event
public record HighSeverityIncidentReported(Guid IncidentId, Guid ProjectId, IncidentSeverity Severity, string Title) : DomainEvent;