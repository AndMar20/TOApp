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
    private ProductFormModel _productForm = new();
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

        ReceiptLines = new ObservableCollection<DocumentLineFormModel>();
        SaleLines = new ObservableCollection<DocumentLineFormModel>();

        _productForm.PropertyChanged += ProductForm_PropertyChanged;

        LoadCommand = new AsyncRelayCommand(_ => LoadAllAsync());
        SaveProductCommand = new AsyncRelayCommand(_ => SaveProductAsync(), _ => SelectedProduct is not null);
        NewProductCommand = new RelayCommand(_ => CreateNewProduct());
        DeleteProductCommand = new AsyncRelayCommand(_ => DeactivateProductAsync(), _ => SelectedProduct is not null && SelectedProduct.ProductId > 0);

        AddSupplierCommand = new AsyncRelayCommand(_ => AddSupplierAsync());
        AddCategoryCommand = new AsyncRelayCommand(_ => AddCategoryAsync());
        AddUnitCommand = new AsyncRelayCommand(_ => AddUnitAsync());

        AddReceiptLineCommand = new RelayCommand(_ => AddReceiptLine());
        RemoveReceiptLineCommand = new RelayCommand(_ => RemoveReceiptLine(), _ => SelectedReceiptLine is not null);
        CreateReceiptCommand = new AsyncRelayCommand(_ => CreateReceiptAsync(), _ => SelectedReceiptSupplier is not null && ReceiptLines.Any() && ReceiptLines.All(IsLineValid));

        AddSaleLineCommand = new RelayCommand(_ => AddSaleLine());
        RemoveSaleLineCommand = new RelayCommand(_ => RemoveSaleLine(), _ => SelectedSaleLine is not null);
        CreateSaleCommand = new AsyncRelayCommand(_ => CreateSaleAsync(), _ => SaleLines.Any() && SaleLines.All(IsLineValid));

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
            if (ReferenceEquals(_productForm, value))
            {
                return;
            }

            _productForm.PropertyChanged -= ProductForm_PropertyChanged;
            _productForm = value;
            _productForm.PropertyChanged += ProductForm_PropertyChanged;
            OnPropertyChanged();
            SyncFormToSelectedProduct();
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

            SyncDocumentLineProductNames(ReceiptLines);
            SyncDocumentLineProductNames(SaleLines);

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

        SyncFormToSelectedProduct();

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

    private void CreateNewProduct()
    {
        if (Categories.Count == 0 || Units.Count == 0)
        {
            _ = _dialogService.ShowErrorAsync("Сначала добавьте хотя бы одну категорию и единицу измерения.", "Внимание");
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
    }

    private void AddReceiptLine()
    {
        var line = CreateDefaultLine();
        line.PropertyChanged += DocumentLineOnPropertyChanged;
        ReceiptLines.Add(line);
        SelectedReceiptLine = line;
        RaiseCommandStates();
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
        var line = CreateDefaultLine();
        line.PropertyChanged += DocumentLineOnPropertyChanged;
        SaleLines.Add(line);
        SelectedSaleLine = line;
        RaiseCommandStates();
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
        if (SelectedReceiptSupplier is null || ReceiptLines.Count == 0 || ReceiptLines.Any(x => !IsLineValid(x)))
        {
            await _dialogService.ShowErrorAsync("Проверьте поставщика и строки документа.", "Проверка данных");
            return;
        }

        try
        {
            await _inventoryService.CreateReceiptAsync(
                SelectedReceiptSupplier.SupplierId,
                ReceiptLines.Select(ToRequest).ToArray());

            await _dialogService.ShowInfoAsync("Поступление сохранено.", "Успех");
            ResetReceiptLines();
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Ошибка создания поступления: {ex.Message}");
        }
    }

    private async Task CreateSaleAsync()
    {
        if (SaleLines.Count == 0 || SaleLines.Any(x => !IsLineValid(x)))
        {
            await _dialogService.ShowErrorAsync("Проверьте строки документа.", "Проверка данных");
            return;
        }

        try
        {
            await _inventoryService.CreateSaleAsync(SaleLines.Select(ToRequest).ToArray());

            await _dialogService.ShowInfoAsync("Продажа сохранена.", "Успех");
            ResetSaleLines();
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Ошибка создания продажи: {ex.Message}");
        }
    }

    private void ProductForm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        SyncFormToSelectedProduct();
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

    private void SyncFormToSelectedProduct()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        SelectedProduct.Sku = ProductForm.Sku;
        SelectedProduct.ProductName = ProductForm.ProductName;
        SelectedProduct.CategoryId = ProductForm.CategoryId;
        SelectedProduct.UnitId = ProductForm.UnitId;
        SelectedProduct.MinStockLevel = ProductForm.MinStockLevel;
        SelectedProduct.Description = ProductForm.Description;
        SelectedProduct.IsActive = ProductForm.IsActive;
    }

    private void SyncDocumentLineProductNames(IEnumerable<DocumentLineFormModel> lines)
    {
        foreach (var line in lines)
        {
            var product = Products.FirstOrDefault(x => x.ProductId == line.ProductId);
            line.ProductName = product?.ProductName ?? string.Empty;
        }
    }

    private DocumentLineFormModel CreateDefaultLine()
    {
        var product = Products.FirstOrDefault();
        return new DocumentLineFormModel
        {
            ProductId = product?.ProductId ?? 0,
            ProductName = product?.ProductName ?? string.Empty,
            Quantity = 1,
            Price = 0
        };
    }

    private void ResetReceiptLines()
    {
        foreach (var line in ReceiptLines)
        {
            line.PropertyChanged -= DocumentLineOnPropertyChanged;
        }

        ReceiptLines.Clear();
        SelectedReceiptLine = null;
    }

    private void ResetSaleLines()
    {
        foreach (var line in SaleLines)
        {
            line.PropertyChanged -= DocumentLineOnPropertyChanged;
        }

        SaleLines.Clear();
        SelectedSaleLine = null;
    }

    private static bool IsLineValid(DocumentLineFormModel line)
        => line.ProductId > 0 && line.Quantity > 0 && line.Price > 0;

    private static DocumentLineRequest ToRequest(DocumentLineFormModel line)
        => new()
        {
            ProductId = line.ProductId,
            Quantity = line.Quantity,
            Price = line.Price
        };

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
