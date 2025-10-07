using Engitrack.BuildingBlocks.Domain;

namespace Engitrack.Inventory.Domain.Materials;

public class Material : AggregateRoot
{
    public Guid MaterialId { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public decimal Stock { get; private set; }
    public decimal MinNivel { get; private set; }
    public MaterialStatus Status { get; private set; }

    private Material() { } // EF Constructor

    public Material(Guid projectId, string name, string unit, decimal minNivel)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException("ProjectId is required", nameof(projectId));

        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            throw new ArgumentException("Name is required and must be <= 160 characters", nameof(name));

        if (string.IsNullOrWhiteSpace(unit) || unit.Length > 32)
            throw new ArgumentException("Unit is required and must be <= 32 characters", nameof(unit));

        if (minNivel < 0)
            throw new ArgumentException("MinNivel must be >= 0", nameof(minNivel));

        MaterialId = Guid.NewGuid();
        ProjectId = projectId;
        Name = name;
        Unit = unit;
        Stock = 0; // Always start with 0 stock
        MinNivel = minNivel;
        Status = MaterialStatus.ACTIVE;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            throw new ArgumentException("Name is required and must be <= 160 characters", nameof(name));

        Name = name;
        MarkAsUpdated();
    }

    public void UpdateUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit) || unit.Length > 32)
            throw new ArgumentException("Unit is required and must be <= 32 characters", nameof(unit));

        Unit = unit;
        MarkAsUpdated();
    }

    public void SetMinNivel(decimal minNivel)
    {
        if (minNivel < 0)
            throw new ArgumentException("MinNivel must be >= 0", nameof(minNivel));

        MinNivel = minNivel;
        MarkAsUpdated();
    }

    public void Archive()
    {
        Status = MaterialStatus.ARCHIVED;
        MarkAsUpdated();
    }

    public void Activate()
    {
        Status = MaterialStatus.ACTIVE;
        MarkAsUpdated();
    }

    // Internal method for SP usage only
    public void UpdateStock(decimal newStock)
    {
        if (newStock < 0)
            throw new InvalidOperationException("Stock cannot be negative");

        Stock = newStock;
        MarkAsUpdated();
    }
}

public enum MaterialStatus
{
    ACTIVE,
    ARCHIVED
}