using Engitrack.BuildingBlocks.Domain;
using Engitrack.Inventory.Domain.Enums;

namespace Engitrack.Inventory.Domain.Entities;

public class Transaction : Entity
{
    public Guid MaterialId { get; private set; }
    public TxType TxType { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid? SupplierId { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public DateTime TxDate { get; private set; }

    private Transaction() { } // EF Constructor

    public Transaction(Guid materialId, TxType txType, decimal quantity, Guid? supplierId = null, string? notes = null)
    {
        if (materialId == Guid.Empty)
            throw new ArgumentException("MaterialId is required", nameof(materialId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (notes?.Length > 400)
            throw new ArgumentException("Notes must be <= 400 characters", nameof(notes));

        MaterialId = materialId;
        TxType = txType;
        Quantity = quantity;
        SupplierId = supplierId;
        Notes = notes ?? string.Empty;
        TxDate = DateTime.UtcNow;
    }
}