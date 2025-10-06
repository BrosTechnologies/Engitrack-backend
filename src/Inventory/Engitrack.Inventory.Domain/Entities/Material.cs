using Engitrack.BuildingBlocks.Domain;
using Engitrack.Inventory.Domain.Enums;

namespace Engitrack.Inventory.Domain.Entities;

public class Material : AggregateRoot
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public decimal Stock { get; private set; }
    public decimal MinLevel { get; private set; }
    public MaterialStatus Status { get; private set; }

    private readonly List<Transaction> _transactions = new();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private Material() { } // EF Constructor

    public Material(Guid projectId, string name, string unit, decimal minLevel)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException("ProjectId is required", nameof(projectId));

        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            throw new ArgumentException("Name is required and must be <= 160 characters", nameof(name));

        if (string.IsNullOrWhiteSpace(unit) || unit.Length > 24)
            throw new ArgumentException("Unit is required and must be <= 24 characters", nameof(unit));

        if (minLevel < 0)
            throw new ArgumentException("MinLevel cannot be negative", nameof(minLevel));

        ProjectId = projectId;
        Name = name;
        Unit = unit;
        Stock = 0;
        MinLevel = minLevel;
        Status = MaterialStatus.ACTIVE;
    }

    public void UpdateBasicInfo(string name, string unit, decimal minLevel)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            throw new ArgumentException("Name is required and must be <= 160 characters", nameof(name));

        if (string.IsNullOrWhiteSpace(unit) || unit.Length > 24)
            throw new ArgumentException("Unit is required and must be <= 24 characters", nameof(unit));

        if (minLevel < 0)
            throw new ArgumentException("MinLevel cannot be negative", nameof(minLevel));

        Name = name;
        Unit = unit;
        MinLevel = minLevel;
        MarkAsUpdated();
    }

    public void UpdateStatus(MaterialStatus status)
    {
        Status = status;
        MarkAsUpdated();
    }

    // Esta operación debe hacerse a través del SP para garantizar atomicidad
    internal void AdjustStock(decimal adjustment)
    {
        var newStock = Stock + adjustment;
        if (newStock < 0)
            throw new InvalidOperationException("Stock cannot be negative");

        Stock = newStock;
        MarkAsUpdated();
    }

    public bool IsLowStock() => Stock <= MinLevel;
}