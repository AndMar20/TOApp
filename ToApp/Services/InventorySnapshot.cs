using TOData.Models;

namespace ToApp.Services;

public sealed class InventorySnapshot
{
    public required List<Product> Products { get; init; }
    public required List<Supplier> Suppliers { get; init; }
    public required List<Category> Categories { get; init; }
    public required List<Unit> Units { get; init; }
    public required List<StockBalance> StockBalances { get; init; }
}

public sealed class DocumentLineRequest
{
    public int ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
}
