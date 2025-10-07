using Engitrack.BuildingBlocks.Domain;

namespace Engitrack.Inventory.Domain.Transactions;

public class InventoryTransaction : Entity
{
    public Guid TxId { get; private set; }
    public Guid MaterialId { get; private set; }
    public TxType TxType { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid? SupplierId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime TxDate { get; private set; }

    private InventoryTransaction() { } // EF Constructor

    public InventoryTransaction(Guid materialId, TxType txType, decimal quantity, Guid? supplierId = null, string? notes = null)
    {
        if (materialId == Guid.Empty)
            throw new ArgumentException("MaterialId is required", nameof(materialId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be > 0", nameof(quantity));

        if (!string.IsNullOrEmpty(notes) && notes.Length > 400)
            throw new ArgumentException("Notes must be <= 400 characters", nameof(notes));

        TxId = Guid.NewGuid();
        MaterialId = materialId;
        TxType = txType;
        Quantity = quantity;
        SupplierId = supplierId;
        Notes = notes;
        TxDate = DateTime.UtcNow;
    }

    public decimal GetStockImpact()
    {
        return TxType switch
        {
            TxType.ENTRY => Quantity,      // Positive: adds to stock
            TxType.USAGE => -Quantity,     // Negative: subtracts from stock
            TxType.ADJUSTMENT => Quantity, // Can be positive or negative
            _ => throw new InvalidOperationException($"Unknown TxType: {TxType}")
        };
    }
}

public enum TxType
{
    ENTRY,
    USAGE,
    ADJUSTMENT
}