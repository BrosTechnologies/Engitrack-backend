using Engitrack.BuildingBlocks.Domain;
using Engitrack.Projects.Domain.Enums;

namespace Engitrack.Projects.Domain.Entities;

public class ProjectTask : Entity
{
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Enums.TaskStatus Status { get; private set; }
    public DateOnly? DueDate { get; private set; }

    private ProjectTask() { } // EF Constructor

    public ProjectTask(Guid projectId, string title, DateOnly? dueDate = null)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException("ProjectId is required", nameof(projectId));

        if (string.IsNullOrWhiteSpace(title) || title.Length > 160)
            throw new ArgumentException("Title is required and must be <= 160 characters", nameof(title));

        ProjectId = projectId;
        Title = title;
        Status = Enums.TaskStatus.PENDING;
        DueDate = dueDate;
    }

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 160)
            throw new ArgumentException("Title is required and must be <= 160 characters", nameof(title));

        Title = title;
        MarkAsUpdated();
    }

    public void UpdateDueDate(DateOnly? dueDate)
    {
        DueDate = dueDate;
        MarkAsUpdated();
    }

    public void StartProgress()
    {
        if (Status == Enums.TaskStatus.DONE)
            throw new InvalidOperationException("Cannot start progress on a completed task");

        Status = Enums.TaskStatus.IN_PROGRESS;
        MarkAsUpdated();
    }

    public void MarkAsDone()
    {
        Status = Enums.TaskStatus.DONE;
        MarkAsUpdated();
    }

    public void ResetToPending()
    {
        Status = Enums.TaskStatus.PENDING;
        MarkAsUpdated();
    }

    public void UpdateStatus(Enums.TaskStatus newStatus)
    {
        Status = newStatus;
        MarkAsUpdated();
    }
}