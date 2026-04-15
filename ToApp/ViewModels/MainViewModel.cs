using System.Collections.ObjectModel;
using System.Windows.Input;
using TOData.Models;
using ToApp.Infrastructure;
using ToApp.Services;

namespace ToApp.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IInventoryService _inventoryService;
    private readonly IDialogService _dialogService;

    private Product? _selectedProduct;
    private ProductFormModel _productForm = null!;
    private StockBalance? _selectedStockBalance;
    private bool _showOnlyLowStock;
    private string _productSearch = string.Empty;

    private Supplier? _selectedReceiptSupplier;
    private DocumentLineFormModel? _selectedReceiptLine;

    private DocumentLineFormModel? _selectedSaleLine;

    public MainViewModel() : this(new InventoryService(), new DialogService())
    {
    }

    public MainViewModel(IInventoryService inventoryService, IDialogService dialogService)
    {
        ArgumentNullException.ThrowIfNull(inventoryService);
        ArgumentNullException.ThrowIfNull(dialogService);

        _inventoryService = inventoryService;
        _dialogService = dialogService;

        Products = new ObservableCollection<Product>();
        Suppliers = new ObservableCollection<Supplier>();
        Categories = new ObservableCollection<Category>();
        Units = new ObservableCollection<Unit>();
        StockBalances = new ObservableCollection<StockBalance>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAllAsync());
        SaveProductCommand = new AsyncRelayCommand(_ => SaveProductAsync(), _ => SelectedProduct is not null);
        NewProductCommand = new RelayCommand(_ => CreateNewProduct());
        DeleteProductCommand = new AsyncRelayCommand(_ => DeactivateProductAsync(), _ => SelectedProduct is not null && SelectedProduct.ProductId > 0);

        AddSupplierCommand = new AsyncRelayCommand(_ => AddSupplierAsync());
        AddCategoryCommand = new AsyncRelayCommand(_ => AddCategoryAsync());
        AddUnitCommand = new AsyncRelayCommand(_ => AddUnitAsync());

        CreateReceiptCommand = new AsyncRelayCommand(_ => CreateReceiptAsync(), _ => SelectedReceiptProduct is not null && SelectedReceiptSupplier is not null && ReceiptQuantity > 0 && ReceiptPrice > 0);
        CreateSaleCommand = new AsyncRelayCommand(_ => CreateSaleAsync(), _ => SelectedSaleProduct is not null && SaleQuantity > 0 && SalePrice > 0);

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

            _productForm.PropertyChanged -= ProductForm_PropertyChanged;
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
                _ = LoadAllAsync();
            }
        }
    }

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
            var snapshot = await _inventoryService.LoadSnapshotAsync(ShowOnlyLowStock, null);

            Products.ReplaceWith(snapshot.Products);
            Suppliers.ReplaceWith(snapshot.Suppliers);
            Categories.ReplaceWith(snapshot.Categories);
            Units.ReplaceWith(snapshot.Units);
            StockBalances.ReplaceWith(snapshot.StockBalances);

            if (SelectedProduct is not null)
            {
                SelectedProduct = Products.FirstOrDefault(x => x.ProductId == SelectedProduct.ProductId)
                                  ?? Products.FirstOrDefault();
            }
            else
            {
                SelectedProduct = Products.FirstOrDefault();
            }

            SelectedReceiptSupplier = Suppliers.FirstOrDefault(x => x.SupplierId == SelectedReceiptSupplier?.SupplierId)
                                      ?? Suppliers.FirstOrDefault();
            SelectedReceiptProduct = Products.FirstOrDefault(x => x.ProductId == SelectedReceiptProduct?.ProductId)
                                     ?? Products.FirstOrDefault();
            SelectedSaleProduct = Products.FirstOrDefault(x => x.ProductId == SelectedSaleProduct?.ProductId)
                                  ?? Products.FirstOrDefault();

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
        if (SelectedProduct is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedProduct.Sku) || string.IsNullOrWhiteSpace(SelectedProduct.ProductName))
        {
            await _dialogService.ShowErrorAsync("Артикул и наименование обязательны.", "Проверка данных");
            return;
        }

        if (SelectedProduct.CategoryId <= 0 || SelectedProduct.UnitId <= 0)
        {
            await _dialogService.ShowErrorAsync("Выберите категорию и единицу измерения.", "Проверка данных");
            return;
        }

        if (SelectedProduct.MinStockLevel < 0)
        {
            await _dialogService.ShowErrorAsync("Мин. остаток не может быть отрицательным.", "Проверка данных");
            return;
        }

        try
        {
            var saved = await _inventoryService.SaveProductAsync(SelectedProduct);
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

        var confirmed = await _dialogService.ConfirmAsync($"Деактивировать товар \"{SelectedProduct.ProductName}\"?", "Подтверждение");
        if (!confirmed)
        {
            return;
        }

        try
        {
            SelectedProduct.IsActive = false;
            await _inventoryService.SaveProductAsync(SelectedProduct);
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

        SelectedSaleLine.PropertyChanged -= DocumentLineOnPropertyChanged;
        SaleLines.Remove(SelectedSaleLine);
        SelectedSaleLine = SaleLines.FirstOrDefault();
        RaiseCommandStates();
    }

    private async Task CreateReceiptAsync()
    {
        if (SelectedReceiptProduct is null || SelectedReceiptSupplier is null || ReceiptQuantity <= 0 || ReceiptPrice <= 0)
        {
            await _dialogService.ShowErrorAsync("Проверьте товар, поставщика, количество и цену.", "Проверка данных");
            return;
        }

        try
        {
            await _inventoryService.CreateReceiptAsync(
                SelectedReceiptSupplier.SupplierId,
                new[] { new DocumentLineRequest { ProductId = SelectedReceiptProduct.ProductId, Quantity = ReceiptQuantity, Price = ReceiptPrice } });

            await _dialogService.ShowInfoAsync("Поступление сохранено.", "Успех");
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Ошибка создания поступления: {ex.Message}");
        }
    }

    private async Task CreateSaleAsync()
    {
        if (SelectedSaleProduct is null || SaleQuantity <= 0 || SalePrice <= 0)
        {
            await _dialogService.ShowErrorAsync("Проверьте товар, количество и цену.", "Проверка данных");
            return;
        }

        try
        {
            await _inventoryService.CreateSaleAsync(
                new[] { new DocumentLineRequest { ProductId = SelectedSaleProduct.ProductId, Quantity = SaleQuantity, Price = SalePrice } });

            await _dialogService.ShowInfoAsync("Продажа сохранена.", "Успех");
            await LoadAllAsync();
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
            await _dialogService.ShowErrorAsync($"Ошибка создания продажи: {ex.Message}");
        }

        if (e.PropertyName == nameof(DocumentLineFormModel.ProductId))
        {
            var product = Products.FirstOrDefault(x => x.ProductId == line.ProductId);
            line.ProductName = product?.ProductName ?? string.Empty;
        }

        RaiseCommandStates();
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
