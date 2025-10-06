using Engitrack.BuildingBlocks.Domain;
using Engitrack.Projects.Domain.Enums;

namespace Engitrack.Projects.Domain.Entities;

public class Project : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public decimal? Budget { get; private set; }
    public ProjectStatus Status { get; private set; }
    public Guid OwnerUserId { get; private set; }

    private readonly List<ProjectTask> _tasks = new();
    public IReadOnlyCollection<ProjectTask> Tasks => _tasks.AsReadOnly();

    private Project() { } // EF Constructor

    public Project(string name, DateOnly startDate, Guid ownerUserId, decimal? budget = null)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            throw new ArgumentException("Name is required and must be <= 160 characters", nameof(name));

        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("OwnerUserId is required", nameof(ownerUserId));

        Name = name;
        StartDate = startDate;
        Budget = budget;
        Status = ProjectStatus.ACTIVE;
        OwnerUserId = ownerUserId;
    }

    public void UpdateBasicInfo(string name, DateOnly? endDate, decimal? budget)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            throw new ArgumentException("Name is required and must be <= 160 characters", nameof(name));

        Name = name;
        EndDate = endDate;
        Budget = budget;
        MarkAsUpdated();
    }

    public void Pause()
    {
        if (Status == ProjectStatus.COMPLETED)
            throw new InvalidOperationException("Cannot pause a completed project");

        Status = ProjectStatus.PAUSED;
        MarkAsUpdated();
    }

    public void Resume()
    {
        if (Status == ProjectStatus.COMPLETED)
            throw new InvalidOperationException("Cannot resume a completed project");

        Status = ProjectStatus.ACTIVE;
        MarkAsUpdated();
    }

    public void Complete()
    {
        // Regla de negocio: no se puede completar un proyecto con tareas abiertas
        var openTasks = _tasks.Where(t => t.Status != Enums.TaskStatus.DONE).ToList();
        if (openTasks.Any())
        {
            var openTasksCount = openTasks.Count;
            throw new InvalidOperationException($"Cannot complete project with {openTasksCount} open tasks. All tasks must be DONE first.");
        }

        Status = ProjectStatus.COMPLETED;
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        MarkAsUpdated();
    }

    public ProjectTask AddTask(string title, DateOnly? dueDate = null)
    {
        if (Status == ProjectStatus.COMPLETED)
            throw new InvalidOperationException("Cannot add tasks to a completed project");

        var task = new ProjectTask(Id, title, dueDate);
        _tasks.Add(task);
        MarkAsUpdated();
        return task;
    }

    public void RemoveTask(Guid taskId)
    {
        if (Status == ProjectStatus.COMPLETED)
            throw new InvalidOperationException("Cannot remove tasks from a completed project");

        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            _tasks.Remove(task);
            MarkAsUpdated();
        }
    }
}