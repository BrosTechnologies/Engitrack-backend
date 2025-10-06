namespace Engitrack.Inventory.Domain.Enums;

public enum TxType
{
    ENTRY,   // Entrada de material
    USAGE,   // Uso de material
    ADJUSTMENT // Ajuste de inventario
}

public enum MaterialStatus
{
    ACTIVE,
    INACTIVE,
    DISCONTINUED
}