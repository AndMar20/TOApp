using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TOData.Models;
using ToApp.Infrastructure;

namespace ToApp.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private Product? _selectedProduct;
    private Supplier? _selectedSupplier;
    private Category? _selectedCategory;
    private Unit? _selectedUnit;
    private StockBalance? _selectedStockBalance;
    private bool _showOnlyLowStock;
    private Product? _selectedReceiptProduct;
    private Supplier? _selectedReceiptSupplier;
    private int _receiptQuantity = 1;
    private decimal _receiptPrice;
    private Product? _selectedSaleProduct;
    private int _saleQuantity = 1;
    private decimal _salePrice;

    public MainViewModel()
    {
        Products = new ObservableCollection<Product>();
        Suppliers = new ObservableCollection<Supplier>();
        Categories = new ObservableCollection<Category>();
        Units = new ObservableCollection<Unit>();
        StockBalances = new ObservableCollection<StockBalance>();

        SaveProductCommand = new RelayCommand(_ => SaveProduct(), _ => SelectedProduct is not null);
        NewProductCommand = new RelayCommand(_ => CreateNewProduct());
        DeleteProductCommand = new RelayCommand(_ => DeleteProduct(), _ => SelectedProduct is not null && SelectedProduct.ProductId > 0);

        AddSupplierCommand = new RelayCommand(_ => AddSupplier());
        AddCategoryCommand = new RelayCommand(_ => AddCategory());
        AddUnitCommand = new RelayCommand(_ => AddUnit());

        CreateReceiptCommand = new RelayCommand(_ => CreateReceipt(), _ => SelectedReceiptProduct is not null && SelectedReceiptSupplier is not null && ReceiptQuantity > 0 && ReceiptPrice > 0);
        CreateSaleCommand = new RelayCommand(_ => CreateSale(), _ => SelectedSaleProduct is not null && SaleQuantity > 0 && SalePrice > 0);

        LoadAll();
    }

    public ObservableCollection<Product> Products { get; }
    public ObservableCollection<Supplier> Suppliers { get; }
    public ObservableCollection<Category> Categories { get; }
    public ObservableCollection<Unit> Units { get; }
    public ObservableCollection<StockBalance> StockBalances { get; }

    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set => SetProperty(ref _selectedSupplier, value);
    }

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public Unit? SelectedUnit
    {
        get => _selectedUnit;
        set => SetProperty(ref _selectedUnit, value);
    }

    public StockBalance? SelectedStockBalance
    {
        get => _selectedStockBalance;
        set => SetProperty(ref _selectedStockBalance, value);
    }

    public Product? SelectedReceiptProduct
    {
        get => _selectedReceiptProduct;
        set
        {
            if (SetProperty(ref _selectedReceiptProduct, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public Supplier? SelectedReceiptSupplier
    {
        get => _selectedReceiptSupplier;
        set
        {
            if (SetProperty(ref _selectedReceiptSupplier, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public int ReceiptQuantity
    {
        get => _receiptQuantity;
        set
        {
            if (SetProperty(ref _receiptQuantity, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public decimal ReceiptPrice
    {
        get => _receiptPrice;
        set
        {
            if (SetProperty(ref _receiptPrice, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public Product? SelectedSaleProduct
    {
        get => _selectedSaleProduct;
        set
        {
            if (SetProperty(ref _selectedSaleProduct, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public int SaleQuantity
    {
        get => _saleQuantity;
        set
        {
            if (SetProperty(ref _saleQuantity, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public decimal SalePrice
    {
        get => _salePrice;
        set
        {
            if (SetProperty(ref _salePrice, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool ShowOnlyLowStock
    {
        get => _showOnlyLowStock;
        set
        {
            if (SetProperty(ref _showOnlyLowStock, value))
            {
                LoadStock();
            }
        }
    }

    public ICommand SaveProductCommand { get; }
    public ICommand NewProductCommand { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand AddSupplierCommand { get; }
    public ICommand AddCategoryCommand { get; }
    public ICommand AddUnitCommand { get; }
    public ICommand CreateReceiptCommand { get; }
    public ICommand CreateSaleCommand { get; }

    public int ProductCount => Products.Count;
    public int SupplierCount => Suppliers.Count;
    public int LowStockCount => StockBalances.Count(x => (x.CurrentStock ?? 0) <= x.MinStockLevel);

    private void LoadAll()
    {
        try
        {
            using var db = new TodbContext();

            Products.ReplaceWith(db.Products.AsNoTracking().OrderBy(x => x.ProductName).ToList());
            Suppliers.ReplaceWith(db.Suppliers.AsNoTracking().OrderBy(x => x.SupplierName).ToList());
            Categories.ReplaceWith(db.Categories.AsNoTracking().OrderBy(x => x.CategoryName).ToList());
            Units.ReplaceWith(db.Units.AsNoTracking().OrderBy(x => x.UnitName).ToList());

            if (SelectedProduct is null && Products.Count > 0)
            {
                SelectedProduct = CloneProduct(Products[0]);
            }

            if (SelectedReceiptSupplier is null && Suppliers.Count > 0)
            {
                SelectedReceiptSupplier = Suppliers[0];
            }

            if (SelectedReceiptProduct is null && Products.Count > 0)
            {
                SelectedReceiptProduct = Products[0];
            }

            if (SelectedSaleProduct is null && Products.Count > 0)
            {
                SelectedSaleProduct = Products[0];
            }

            LoadStock(db);
            OnPropertyChanged(nameof(ProductCount));
            OnPropertyChanged(nameof(SupplierCount));
            OnPropertyChanged(nameof(LowStockCount));
            RaiseCommandStates();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadStock(TodbContext? dbContext = null)
    {
        var ownContext = dbContext is null;
        using var db = ownContext ? new TodbContext() : null;
        var context = dbContext ?? db!;

        var query = context.StockBalances.AsNoTracking().OrderBy(x => x.ProductName).AsQueryable();
        if (ShowOnlyLowStock)
        {
            query = query.Where(x => (x.CurrentStock ?? 0) <= x.MinStockLevel);
        }

        StockBalances.ReplaceWith(query.ToList());
        OnPropertyChanged(nameof(LowStockCount));
    }

    private void SaveProduct()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        try
        {
            using var db = new TodbContext();
            Product entity;
            if (SelectedProduct.ProductId == 0)
            {
                entity = new Product();
                db.Products.Add(entity);
            }
            else
            {
                entity = db.Products.First(x => x.ProductId == SelectedProduct.ProductId);
            }

            entity.Sku = SelectedProduct.Sku;
            entity.ProductName = SelectedProduct.ProductName;
            entity.CategoryId = SelectedProduct.CategoryId;
            entity.UnitId = SelectedProduct.UnitId;
            entity.MinStockLevel = SelectedProduct.MinStockLevel;
            entity.Description = SelectedProduct.Description;
            entity.IsActive = SelectedProduct.IsActive;

            db.SaveChanges();
            LoadAll();
            MessageBox.Show("Товар сохранен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateNewProduct()
    {
        if (Categories.Count == 0 || Units.Count == 0)
        {
            MessageBox.Show("Сначала добавьте хотя бы одну категорию и единицу измерения.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedProduct = new Product
        {
            IsActive = true,
            CategoryId = Categories[0].CategoryId,
            UnitId = Units[0].UnitId,
            MinStockLevel = 0,
            Sku = string.Empty,
            ProductName = string.Empty
        };
    }

    private void DeleteProduct()
    {
        if (SelectedProduct is null || SelectedProduct.ProductId == 0)
        {
            return;
        }

        try
        {
            using var db = new TodbContext();
            var entity = db.Products.First(x => x.ProductId == SelectedProduct.ProductId);
            db.Products.Remove(entity);
            db.SaveChanges();
            SelectedProduct = null;
            LoadAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось удалить товар: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddSupplier()
    {
        var name = Prompt("Новый поставщик", "Введите название поставщика:");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            using var db = new TodbContext();
            db.Suppliers.Add(new Supplier { SupplierName = name.Trim() });
            db.SaveChanges();
            LoadAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка добавления поставщика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddCategory()
    {
        var name = Prompt("Новая категория", "Введите название категории:");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            using var db = new TodbContext();
            db.Categories.Add(new Category { CategoryName = name.Trim() });
            db.SaveChanges();
            LoadAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка добавления категории: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddUnit()
    {
        var name = Prompt("Новая единица", "Введите название единицы:");
        var shortName = Prompt("Новая единица", "Введите краткое обозначение:");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(shortName))
        {
            return;
        }

        try
        {
            using var db = new TodbContext();
            db.Units.Add(new Unit { UnitName = name.Trim(), UnitShortName = shortName.Trim() });
            db.SaveChanges();
            LoadAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка добавления единицы измерения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateReceipt()
    {
        if (SelectedReceiptProduct is null || SelectedReceiptSupplier is null)
        {
            return;
        }

        try
        {
            using var db = new TodbContext();
            var receipt = new Receipt
            {
                SupplierId = SelectedReceiptSupplier.SupplierId,
                ReceiptDate = DateTime.Now,
                TotalAmount = ReceiptQuantity * ReceiptPrice,
                ReceiptItems =
                {
                    new ReceiptItem
                    {
                        ProductId = SelectedReceiptProduct.ProductId,
                        Quantity = ReceiptQuantity,
                        Price = ReceiptPrice
                    }
                }
            };

            db.Receipts.Add(receipt);
            db.SaveChanges();

            MessageBox.Show("Поступление сохранено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка создания поступления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateSale()
    {
        if (SelectedSaleProduct is null)
        {
            return;
        }

        try
        {
            using var db = new TodbContext();
            var stock = db.StockBalances.AsNoTracking().FirstOrDefault(x => x.ProductId == SelectedSaleProduct.ProductId)?.CurrentStock ?? 0;
            if (stock < SaleQuantity)
            {
                MessageBox.Show($"Недостаточно остатка. Доступно: {stock}", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sale = new Sale
            {
                SaleDate = DateTime.Now,
                TotalAmount = SaleQuantity * SalePrice,
                SaleItems =
                {
                    new SaleItem
                    {
                        ProductId = SelectedSaleProduct.ProductId,
                        Quantity = SaleQuantity,
                        Price = SalePrice
                    }
                }
            };

            db.Sales.Add(sale);
            db.SaveChanges();

            MessageBox.Show("Продажа сохранена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка создания продажи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static Product CloneProduct(Product source)
    {
        return new Product
        {
            ProductId = source.ProductId,
            Sku = source.Sku,
            ProductName = source.ProductName,
            CategoryId = source.CategoryId,
            UnitId = source.UnitId,
            MinStockLevel = source.MinStockLevel,
            Description = source.Description,
            IsActive = source.IsActive
        };
    }

    private void RaiseCommandStates()
    {
        RaiseCanExecute(SaveProductCommand, DeleteProductCommand, CreateReceiptCommand, CreateSaleCommand);
    }

    private static void RaiseCanExecute(params ICommand[] commands)
    {
        foreach (var command in commands.OfType<RelayCommand>())
        {
            command.RaiseCanExecuteChanged();
        }
    }

    private static string? Prompt(string title, string message)
    {
        return Microsoft.VisualBasic.Interaction.InputBox(message, title);
    }
}

public static class ObservableCollectionExtensions
{
    public static void ReplaceWith<T>(this ObservableCollection<T> collection, IReadOnlyCollection<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
