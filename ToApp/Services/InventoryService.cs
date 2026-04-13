using Microsoft.EntityFrameworkCore;
using TOData.Models;

namespace ToApp.Services;

public sealed class InventoryService : IInventoryService
{
    public async Task<InventorySnapshot> LoadSnapshotAsync(bool onlyLowStock, string? productSearch)
    {
        await using var db = new TodbContext();

        var productsQuery = db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(productSearch))
        {
            var search = productSearch.Trim();
            productsQuery = productsQuery.Where(x => x.ProductName.Contains(search) || x.Sku.Contains(search));
        }

        var stockQuery = db.StockBalances.AsNoTracking().AsQueryable();
        if (onlyLowStock)
        {
            stockQuery = stockQuery.Where(x => (x.CurrentStock ?? 0) <= x.MinStockLevel);
        }

        return new InventorySnapshot
        {
            Products = await productsQuery.OrderBy(x => x.ProductName).ToListAsync(),
            Suppliers = await db.Suppliers.AsNoTracking().OrderBy(x => x.SupplierName).ToListAsync(),
            Categories = await db.Categories.AsNoTracking().OrderBy(x => x.CategoryName).ToListAsync(),
            Units = await db.Units.AsNoTracking().OrderBy(x => x.UnitName).ToListAsync(),
            StockBalances = await stockQuery.OrderBy(x => x.ProductName).ToListAsync()
        };
    }

    public async Task<Product> SaveProductAsync(Product product)
    {
        await using var db = new TodbContext();
        Product entity;

        if (product.ProductId == 0)
        {
            entity = new Product();
            db.Products.Add(entity);
        }
        else
        {
            entity = await db.Products.FirstAsync(x => x.ProductId == product.ProductId);
        }

        entity.Sku = product.Sku.Trim();
        entity.ProductName = product.ProductName.Trim();
        entity.CategoryId = product.CategoryId;
        entity.UnitId = product.UnitId;
        entity.MinStockLevel = product.MinStockLevel;
        entity.Description = product.Description?.Trim();
        entity.IsActive = product.IsActive;

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteProductAsync(int productId)
    {
        await using var db = new TodbContext();
        var entity = await db.Products.FirstAsync(x => x.ProductId == productId);
        db.Products.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task AddSupplierAsync(string supplierName)
    {
        await using var db = new TodbContext();
        db.Suppliers.Add(new Supplier { SupplierName = supplierName.Trim() });
        await db.SaveChangesAsync();
    }

    public async Task AddCategoryAsync(string categoryName)
    {
        await using var db = new TodbContext();
        db.Categories.Add(new Category { CategoryName = categoryName.Trim() });
        await db.SaveChangesAsync();
    }

    public async Task AddUnitAsync(string unitName, string shortName)
    {
        await using var db = new TodbContext();
        db.Units.Add(new Unit { UnitName = unitName.Trim(), UnitShortName = shortName.Trim() });
        await db.SaveChangesAsync();
    }

    public async Task CreateReceiptAsync(int supplierId, IReadOnlyCollection<DocumentLineRequest> lines)
    {
        await using var db = new TodbContext();
        await using var tx = await db.Database.BeginTransactionAsync();

        var receipt = new Receipt
        {
            SupplierId = supplierId,
            ReceiptDate = DateTime.Now,
            TotalAmount = lines.Sum(x => x.Quantity * x.Price)
        };
        db.Receipts.Add(receipt);
        await db.SaveChangesAsync();

        foreach (var line in lines)
        {
            db.ReceiptItems.Add(new ReceiptItem
            {
                ReceiptId = receipt.ReceiptId,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                Price = line.Price
            });
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task CreateSaleAsync(IReadOnlyCollection<DocumentLineRequest> lines)
    {
        await using var db = new TodbContext();
        await using var tx = await db.Database.BeginTransactionAsync();

        var lineByProduct = lines.GroupBy(x => x.ProductId).ToDictionary(x => x.Key, x => x.Sum(i => i.Quantity));
        var stockMap = await db.StockBalances.AsNoTracking()
            .Where(x => lineByProduct.Keys.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId, x => x.CurrentStock ?? 0);

        foreach (var (productId, qty) in lineByProduct)
        {
            var stock = stockMap.GetValueOrDefault(productId, 0);
            if (stock < qty)
            {
                throw new InvalidOperationException($"Недостаточно остатка для товара #{productId}. Доступно: {stock}, требуется: {qty}.");
            }
        }

        var sale = new Sale
        {
            SaleDate = DateTime.Now,
            TotalAmount = lines.Sum(x => x.Quantity * x.Price)
        };
        db.Sales.Add(sale);
        await db.SaveChangesAsync();

        foreach (var line in lines)
        {
            db.SaleItems.Add(new SaleItem
            {
                SaleId = sale.SaleId,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                Price = line.Price
            });
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
