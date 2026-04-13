using TOData.Models;

namespace ToApp.Services;

public interface IInventoryService
{
    Task<InventorySnapshot> LoadSnapshotAsync(bool onlyLowStock, string? productSearch);
    Task<Product> SaveProductAsync(Product product);
    Task DeleteProductAsync(int productId);
    Task AddSupplierAsync(string supplierName);
    Task AddCategoryAsync(string categoryName);
    Task AddUnitAsync(string unitName, string shortName);
    Task CreateReceiptAsync(int supplierId, IReadOnlyCollection<DocumentLineRequest> lines);
    Task CreateSaleAsync(IReadOnlyCollection<DocumentLineRequest> lines);
}
