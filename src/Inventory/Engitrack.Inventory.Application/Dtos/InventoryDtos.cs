namespace Engitrack.Inventory.Application.Dtos;

public record RegisterTransactionRequest(
    Guid MaterialId,
    string TxType,
    decimal Quantity,
    Guid? SupplierId = null,
    string? Notes = null);

public record TransactionResponse(
    Guid TxId,
    Guid MaterialId,
    string TxType,
    decimal Quantity,
    Guid? SupplierId,
    string? Notes,
    DateTime TxDate);