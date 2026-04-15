using System.Collections.ObjectModel;
using System.Windows.Input;
using TOData.Models;
using ToApp.Infrastructure;
using ToApp.Services;
using ToApp.ViewModels.Forms;

namespace ToApp.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IInventoryService _inventoryService;
    private readonly IDialogService _dialogService;

    private Product? _selectedProduct;
    private ProductFormModel _productForm = new();
    private StockBalance? _selectedStockBalance;
    private bool _showOnlyLowStock;
    private string _productSearch = string.Empty;

    private Supplier? _selectedReceiptSupplier;
    private DocumentLineFormModel? _selectedReceiptLine;

    private DocumentLineFormModel? _selectedSaleLine;

    public MainViewModel(IInventoryService inventoryService, IDialogService dialogService)
    {
        _inventoryService = inventoryService;
        _dialogService = dialogService;

        Products = new ObservableCollection<Product>();
        Suppliers = new ObservableCollection<Supplier>();
        Categories = new ObservableCollection<Category>();
        Units = new ObservableCollection<Unit>();
        StockBalances = new ObservableCollection<StockBalance>();

        ReceiptLines = new ObservableCollection<DocumentLineFormModel>();
        SaleLines = new ObservableCollection<DocumentLineFormModel>();
        _productForm.PropertyChanged += ProductForm_PropertyChanged;

        LoadCommand = new AsyncRelayCommand(_ => LoadAllAsync());
        SaveProductCommand = new AsyncRelayCommand(_ => SaveProductAsync(), _ => ProductForm.IsValid);
        NewProductCommand = new RelayCommand(_ => CreateNewProduct());
        DeleteProductCommand = new AsyncRelayCommand(_ => DeactivateProductAsync(), _ => SelectedProduct?.ProductId > 0);

        AddSupplierCommand = new AsyncRelayCommand(_ => AddSupplierAsync());
        AddCategoryCommand = new AsyncRelayCommand(_ => AddCategoryAsync());
        AddUnitCommand = new AsyncRelayCommand(_ => AddUnitAsync());

        AddReceiptLineCommand = new RelayCommand(_ => AddReceiptLine());
        RemoveReceiptLineCommand = new RelayCommand(_ => RemoveReceiptLine(), _ => SelectedReceiptLine is not null);
        CreateReceiptCommand = new AsyncRelayCommand(_ => CreateReceiptAsync(), _ => CanCreateReceipt());

        AddSaleLineCommand = new RelayCommand(_ => AddSaleLine());
        RemoveSaleLineCommand = new RelayCommand(_ => RemoveSaleLine(), _ => SelectedSaleLine is not null);
        CreateSaleCommand = new AsyncRelayCommand(_ => CreateSaleAsync(), _ => CanCreateSale());

        ReceiptLines.CollectionChanged += (_, _) => RaiseCommandStates();
        SaleLines.CollectionChanged += (_, _) => RaiseCommandStates();

        _ = LoadAllAsync();
    }

    public ObservableCollection<Product> Products { get; }
    public ObservableCollection<Supplier> Suppliers { get; }
    public ObservableCollection<Category> Categories { get; }
    public ObservableCollection<Unit> Units { get; }
    public ObservableCollection<StockBalance> StockBalances { get; }

    public ObservableCollection<DocumentLineFormModel> ReceiptLines { get; }
    public ObservableCollection<DocumentLineFormModel> SaleLines { get; }

    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                ProductForm = value is null ? new ProductFormModel() : ProductFormModel.FromProduct(value);
                RaiseCommandStates();
            }
        }
    }

    public ProductFormModel ProductForm
    {
        get => _productForm;
        private set
        {
            if (_productForm == value)
            {
                return;
            }

            if (_productForm is not null)
            {
                _productForm.PropertyChanged -= ProductForm_PropertyChanged;
            }
            _productForm = value;
            _productForm.PropertyChanged += ProductForm_PropertyChanged;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    public StockBalance? SelectedStockBalance
    {
        get => _selectedStockBalance;
        set => SetProperty(ref _selectedStockBalance, value);
    }

    public bool ShowOnlyLowStock
    {
        get => _showOnlyLowStock;
        set
        {
            if (SetProperty(ref _showOnlyLowStock, value))
            {
                _ = LoadAllAsync();
            }
        }
    }

    public string ProductSearch
    {
        get => _productSearch;
        set
        {
            if (SetProperty(ref _productSearch, value))
            {
                _ = LoadAllAsync();
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

    public DocumentLineFormModel? SelectedReceiptLine
    {
        get => _selectedReceiptLine;
        set
        {
            if (SetProperty(ref _selectedReceiptLine, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public DocumentLineFormModel? SelectedSaleLine
    {
        get => _selectedSaleLine;
        set
        {
            if (SetProperty(ref _selectedSaleLine, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public ICommand LoadCommand { get; }
    public ICommand SaveProductCommand { get; }
    public ICommand NewProductCommand { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand AddSupplierCommand { get; }
    public ICommand AddCategoryCommand { get; }
    public ICommand AddUnitCommand { get; }
    public ICommand AddReceiptLineCommand { get; }
    public ICommand RemoveReceiptLineCommand { get; }
    public ICommand CreateReceiptCommand { get; }
    public ICommand AddSaleLineCommand { get; }
    public ICommand RemoveSaleLineCommand { get; }
    public ICommand CreateSaleCommand { get; }

    public int ProductCount => Products.Count;
    public int SupplierCount => Suppliers.Count;
    public int LowStockCount => StockBalances.Count(x => (x.CurrentStock ?? 0) <= x.MinStockLevel);

    private async Task LoadAllAsync()
    {
        try
        {
            var snapshot = await _inventoryService.LoadSnapshotAsync(ShowOnlyLowStock, ProductSearch);

            Products.ReplaceWith(snapshot.Products);
            Suppliers.ReplaceWith(snapshot.Suppliers);
            Categories.ReplaceWith(snapshot.Categories);
            Units.ReplaceWith(snapshot.Units);
            StockBalances.ReplaceWith(snapshot.StockBalances);

            if (SelectedProduct is null || Products.All(x => x.ProductId != SelectedProduct.ProductId))
            {
                SelectedProduct = Products.FirstOrDefault();
            }

            if (SelectedReceiptSupplier is null || Suppliers.All(x => x.SupplierId != SelectedReceiptSupplier.SupplierId))
            {
                SelectedReceiptSupplier = Suppliers.FirstOrDefault();
            }

            SyncDocumentProducts(ReceiptLines);
            SyncDocumentProducts(SaleLines);

            if (ReceiptLines.Count == 0)
            {
                AddReceiptLine();
            }

            if (SaleLines.Count == 0)
            {
                AddSaleLine();
            }

            OnPropertyChanged(nameof(ProductCount));
            OnPropertyChanged(nameof(SupplierCount));
            OnPropertyChanged(nameof(LowStockCount));
            RaiseCommandStates();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Ошибка загрузки данных: {ex.Message}");
        }
    }

    private async Task SaveProductAsync()
    {
        if (!ProductForm.IsValid)
        {
            await _dialogService.ShowErrorAsync("Проверьте корректность полей карточки товара.", "Проверка данных");
            return;
        }

        try
        {
            var saved = await _inventoryService.SaveProductAsync(ProductForm.ToProduct());
            await _dialogService.ShowInfoAsync("Товар сохранен.", "Успех");
            await LoadAllAsync();
            SelectedProduct = Products.FirstOrDefault(x => x.ProductId == saved.ProductId);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Ошибка сохранения товара: {ex.Message}");
        }
    }

    // Backward-compatible wrapper: keeps references that still call SaveProduct().
    private Task SaveProduct() => SaveProductAsync();

    private void CreateNewProduct()
    {
        if (Categories.Count == 0 || Units.Count == 0)
        {
            _ = _dialogService.ShowErrorAsync("Сначала добавьте хотя бы одну категорию и единицу измерения.", "Внимание");
            return;
        }

        SelectedProduct = null;
        ProductForm = new ProductFormModel
        {
            IsActive = true,
            CategoryId = Categories[0].CategoryId,
            UnitId = Units[0].UnitId,
            MinStockLevel = 0,
            Sku = string.Empty,
            ProductName = string.Empty
        };
    }

    private async Task DeactivateProductAsync()
    {
        if (SelectedProduct is null || SelectedProduct.ProductId == 0)
        {
            return;
        }

        var confirmed = await _dialogService.ConfirmAsync(
            $"Деактивировать товар \"{SelectedProduct.ProductName}\"?",
            "Подтверждение"
        );
        if (!confirmed)
        {
            return;
        }

        try
        {
            var product = ProductFormModel.FromProduct(SelectedProduct);
            product.IsActive = false;
            await _inventoryService.SaveProductAsync(product.ToProduct());
            await _dialogService.ShowInfoAsync("Товар деактивирован.", "Успех");
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Не удалось деактивировать товар: {ex.Message}");
        }
    }

    private async Task AddSupplierAsync()
    {
        var name = await _dialogService.PromptAsync("Введите название поставщика:", "Новый поставщик");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            await _inventoryService.AddSupplierAsync(name);
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Ошибка добавления поставщика: {ex.Message}");
        }
    }

    private async Task AddCategoryAsync()
    {
        var name = await _dialogService.PromptAsync("Введите название категории:", "Новая категория");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            await _inventoryService.AddCategoryAsync(name);
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Ошибка добавления категории: {ex.Message}");
        }
    }

    private async Task AddUnitAsync()
    {
        var name = await _dialogService.PromptAsync("Введите название единицы:", "Новая единица");
        var shortName = await _dialogService.PromptAsync("Введите краткое обозначение:", "Новая единица");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(shortName))
        {
            return;
        }

        try
        {
            await _inventoryService.AddUnitAsync(name, shortName);
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Ошибка добавления единицы измерения: {ex.Message}");
        }
    }

    private void AddReceiptLine()
    {
        var product = Products.FirstOrDefault();
        var line = new DocumentLineFormModel
        {
            ProductId = product?.ProductId ?? 0,
            ProductName = product?.ProductName ?? string.Empty,
            Quantity = 1,
            Price = 0m
        };
        line.PropertyChanged += DocumentLineOnPropertyChanged;
        ReceiptLines.Add(line);
        SelectedReceiptLine = line;
    }

    private void RemoveReceiptLine()
    {
        if (SelectedReceiptLine is null)
        {
            return;
        }

        SelectedReceiptLine.PropertyChanged -= DocumentLineOnPropertyChanged;
        ReceiptLines.Remove(SelectedReceiptLine);
        SelectedReceiptLine = ReceiptLines.FirstOrDefault();
        RaiseCommandStates();
    }

    private void AddSaleLine()
    {
        var product = Products.FirstOrDefault();
        var line = new DocumentLineFormModel
        {
            ProductId = product?.ProductId ?? 0,
            ProductName = product?.ProductName ?? string.Empty,
            Quantity = 1,
            Price = 0m
        };
        line.PropertyChanged += DocumentLineOnPropertyChanged;
        SaleLines.Add(line);
        SelectedSaleLine = line;
    }

    private void RemoveSaleLine()
    {
        if (SelectedSaleLine is null)
        {
            return;
        }

        SelectedSaleLine.PropertyChanged -= DocumentLineOnPropertyChanged;
        SaleLines.Remove(SelectedSaleLine);
        SelectedSaleLine = SaleLines.FirstOrDefault();
        RaiseCommandStates();
    }

    private async Task CreateReceiptAsync()
    {
        if (!CanCreateReceipt())
        {
            await _dialogService.ShowErrorAsync("Проверьте строки поступления и поставщика.", "Проверка данных");
            return;
        }

        try
        {
            var lines = ReceiptLines
                .Select(x => new DocumentLineRequest { ProductId = x.ProductId, Quantity = x.Quantity, Price = x.Price })
                .ToList();

            await _inventoryService.CreateReceiptAsync(SelectedReceiptSupplier!.SupplierId, lines);
            await _dialogService.ShowInfoAsync("Поступление сохранено.", "Успех");

            ClearDocumentLines(ReceiptLines);
            AddReceiptLine();
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Ошибка создания поступления: {ex.Message}");
        }
    }

    private async Task CreateSaleAsync()
    {
        if (!CanCreateSale())
        {
            await _dialogService.ShowErrorAsync("Проверьте строки продажи.", "Проверка данных");
            return;
        }

        try
        {
            var lines = SaleLines
                .Select(x => new DocumentLineRequest { ProductId = x.ProductId, Quantity = x.Quantity, Price = x.Price })
                .ToList();

            await _inventoryService.CreateSaleAsync(lines);
            await _dialogService.ShowInfoAsync("Продажа сохранена.", "Успех");

            ClearDocumentLines(SaleLines);
            AddSaleLine();
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Ошибка создания продажи: {ex.Message}");
        }
    }

    private bool CanCreateReceipt()
    {
        return SelectedReceiptSupplier is not null
               && ReceiptLines.Count > 0
               && ReceiptLines.All(x => x.IsValid);
    }

    private bool CanCreateSale()
    {
        return SaleLines.Count > 0
               && SaleLines.All(x => x.IsValid);
    }

    private void SyncDocumentProducts(IEnumerable<DocumentLineFormModel> lines)
    {
        foreach (var line in lines)
        {
            if (line.ProductId == 0)
            {
                continue;
            }

            var product = Products.FirstOrDefault(x => x.ProductId == line.ProductId);
            if (product is null)
            {
                line.ProductId = Products.FirstOrDefault()?.ProductId ?? 0;
                line.ProductName = Products.FirstOrDefault()?.ProductName ?? string.Empty;
                continue;
            }

            line.ProductName = product.ProductName;
        }
    }

    private void ProductForm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        RaiseCommandStates();
    }

    private void DocumentLineOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not DocumentLineFormModel line)
        {
            return;
        }

        if (e.PropertyName == nameof(DocumentLineFormModel.ProductId))
        {
            var product = Products.FirstOrDefault(x => x.ProductId == line.ProductId);
            line.ProductName = product?.ProductName ?? string.Empty;
        }

        RaiseCommandStates();
    }

    private void ClearDocumentLines(ICollection<DocumentLineFormModel> lines)
    {
        foreach (var line in lines)
        {
            line.PropertyChanged -= DocumentLineOnPropertyChanged;
        }

        lines.Clear();
    }

    private void RaiseCommandStates()
    {
        RaiseCanExecute(SaveProductCommand, DeleteProductCommand, RemoveReceiptLineCommand, CreateReceiptCommand, RemoveSaleLineCommand, CreateSaleCommand);
    }

    private static void RaiseCanExecute(params ICommand[] commands)
    {
        foreach (var relay in commands.OfType<RelayCommand>())
        {
            relay.RaiseCanExecuteChanged();
        }

        foreach (var asyncRelay in commands.OfType<AsyncRelayCommand>())
        {
            asyncRelay.RaiseCanExecuteChanged();
        }
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
