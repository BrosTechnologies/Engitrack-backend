using Engitrack.Inventory.Domain.Entities;
using Engitrack.Inventory.Domain.Enums;

namespace Engitrack.Inventory.Domain.Repositories;

public interface IMaterialRepository
{
    Task<Material?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Material>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Material>> GetLowStockMaterialsAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(Material material, CancellationToken cancellationToken = default);
    Task UpdateAsync(Material material, CancellationToken cancellationToken = default);
    Task DeleteAsync(Material material, CancellationToken cancellationToken = default);
}

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Supplier>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task DeleteAsync(Supplier supplier, CancellationToken cancellationToken = default);
}

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetByMaterialIdAsync(Guid materialId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByMaterialIdAndDateRangeAsync(Guid materialId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
}

public interface IInventoryService
{
    Task RegisterTransactionAsync(Guid materialId, TxType txType, decimal quantity, Guid? supplierId = null, string? notes = null, CancellationToken cancellationToken = default);
}