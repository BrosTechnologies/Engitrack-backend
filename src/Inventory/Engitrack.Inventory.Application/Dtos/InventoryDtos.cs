using Engitrack.Inventory.Domain.Materials;

namespace Engitrack.Inventory.Application.Dtos;

// Material DTOs
public record CreateMaterialRequest(
    Guid ProjectId, 
    string Name, 
    string Unit, 
    decimal MinNivel
);

public record UpdateMaterialRequest(
    string? Name,
    string? Unit,
    decimal? MinNivel,
    bool? Archive
);

public record MaterialDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string Unit,
    decimal Stock,
    decimal MinNivel,
    string Status
);

public record MaterialListResponse(
    List<MaterialDto> Items,
    int Total,
    int Page,
    int PageSize
);

public record MaterialQuery(
    string? Q,
    Guid? ProjectId,
    MaterialStatus? Status,
    int Page = 1,
    int PageSize = 20
);

// Transaction DTOs
public record RegisterTransactionRequest(
    string TxType,
    decimal Quantity,
    Guid? SupplierId,
    string? Notes
);

public record TransactionDto(
    Guid TxId,
    string TxType,
    decimal Quantity,
    DateTime TxDate,
    Guid? SupplierId,
    string? Notes
);

public record TransactionListResponse(
    List<TransactionDto> Items,
    int Total,
    int Page,
    int PageSize
);

// Supplier DTOs
public record CreateSupplierRequest(
    string Name,
    string? Ruc,
    string? Phone,
    string? Email
);

public record UpdateSupplierRequest(
    string? Name,
    string? Ruc,
    string? Phone,
    string? Email
);

public record SupplierDto(
    Guid SupplierId,
    string Name,
    string? Ruc,
    string? Phone,
    string? Email
);

// Legacy DTOs (keeping for compatibility)
public record LegacyRegisterTransactionRequest(
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