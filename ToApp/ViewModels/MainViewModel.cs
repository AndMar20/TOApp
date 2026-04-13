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
    private Supplier? _selectedReceiptSupplier;
    private Product? _lineReceiptProduct;
    private int _lineReceiptQuantity = 1;
    private decimal _lineReceiptPrice;
    private Product? _lineSaleProduct;
    private int _lineSaleQuantity = 1;
    private decimal _lineSalePrice;
    private DocumentLineFormModel? _selectedReceiptLine;
    private DocumentLineFormModel? _selectedSaleLine;
    private bool _showOnlyLowStock;
    private string _productSearch = string.Empty;
    private bool _isBusy;
    private string _statusText = "Готово";

    public MainViewModel()
        : this(new InventoryService(), new DialogService())
    {
    }

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

        ProductForm = new ProductFormModel();

        LoadCommand = new AsyncRelayCommand(_ => LoadAllAsync());
        SaveProductCommand = new AsyncRelayCommand(_ => SaveProductAsync(), _ => !IsBusy && ProductForm.IsValid);
        NewProductCommand = new RelayCommand(_ => CreateNewProduct(), _ => !IsBusy);
        DeleteProductCommand = new AsyncRelayCommand(_ => DeleteProductAsync(), _ => !IsBusy && ProductForm.ProductId > 0);

        AddSupplierCommand = new AsyncRelayCommand(_ => AddSupplierAsync(), _ => !IsBusy);
        AddCategoryCommand = new AsyncRelayCommand(_ => AddCategoryAsync(), _ => !IsBusy);
        AddUnitCommand = new AsyncRelayCommand(_ => AddUnitAsync(), _ => !IsBusy);

        AddReceiptLineCommand = new RelayCommand(_ => AddReceiptLine(), _ => !IsBusy && CanAddReceiptLine());
        RemoveReceiptLineCommand = new RelayCommand(_ => RemoveReceiptLine(), _ => !IsBusy && SelectedReceiptLine is not null);
        CreateReceiptCommand = new AsyncRelayCommand(_ => CreateReceiptAsync(), _ => !IsBusy && SelectedReceiptSupplier is not null && ReceiptLines.Count > 0 && ReceiptLines.All(x => x.IsValid));

        AddSaleLineCommand = new RelayCommand(_ => AddSaleLine(), _ => !IsBusy && CanAddSaleLine());
        RemoveSaleLineCommand = new RelayCommand(_ => RemoveSaleLine(), _ => !IsBusy && SelectedSaleLine is not null);
        CreateSaleCommand = new AsyncRelayCommand(_ => CreateSaleAsync(), _ => !IsBusy && SaleLines.Count > 0 && SaleLines.All(x => x.IsValid));
    }

    public ObservableCollection<Product> Products { get; }
    public ObservableCollection<Supplier> Suppliers { get; }
    public ObservableCollection<Category> Categories { get; }
    public ObservableCollection<Unit> Units { get; }
    public ObservableCollection<StockBalance> StockBalances { get; }

    public ObservableCollection<DocumentLineFormModel> ReceiptLines { get; }
    public ObservableCollection<DocumentLineFormModel> SaleLines { get; }

    public ProductFormModel ProductForm { get; private set; }

    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (!SetProperty(ref _selectedProduct, value) || value is null)
            {
                return;
            }

            ProductForm = ProductFormModel.FromProduct(value);
            OnPropertyChanged(nameof(ProductForm));
            RaiseCommandStates();
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

    public Product? LineReceiptProduct
    {
        get => _lineReceiptProduct;
        set
        {
            if (SetProperty(ref _lineReceiptProduct, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public int LineReceiptQuantity
    {
        get => _lineReceiptQuantity;
        set
        {
            if (SetProperty(ref _lineReceiptQuantity, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public decimal LineReceiptPrice
    {
        get => _lineReceiptPrice;
        set
        {
            if (SetProperty(ref _lineReceiptPrice, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public Product? LineSaleProduct
    {
        get => _lineSaleProduct;
        set
        {
            if (SetProperty(ref _lineSaleProduct, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public int LineSaleQuantity
    {
        get => _lineSaleQuantity;
        set
        {
            if (SetProperty(ref _lineSaleQuantity, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public decimal LineSalePrice
    {
        get => _lineSalePrice;
        set
        {
            if (SetProperty(ref _lineSalePrice, value))
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

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public int ProductCount => Products.Count;
    public int SupplierCount => Suppliers.Count;
    public int LowStockCount => StockBalances.Count(x => (x.CurrentStock ?? 0) <= x.MinStockLevel);
    public decimal ReceiptTotal => ReceiptLines.Sum(x => x.Amount);
    public decimal SaleTotal => SaleLines.Sum(x => x.Amount);

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

    public async Task LoadAllAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusText = "Загрузка данных...";
            var snapshot = await _inventoryService.LoadSnapshotAsync(ShowOnlyLowStock, ProductSearch);

            Products.ReplaceWith(snapshot.Products);
            Suppliers.ReplaceWith(snapshot.Suppliers);
            Categories.ReplaceWith(snapshot.Categories);
            Units.ReplaceWith(snapshot.Units);
            StockBalances.ReplaceWith(snapshot.StockBalances);

            if (SelectedProduct is null && Products.Count > 0)
            {
                SelectedProduct = Products[0];
            }

            if (SelectedReceiptSupplier is null && Suppliers.Count > 0)
            {
                SelectedReceiptSupplier = Suppliers[0];
            }

            LineReceiptProduct ??= Products.FirstOrDefault();
            LineSaleProduct ??= Products.FirstOrDefault();

            OnPropertyChanged(nameof(ProductCount));
            OnPropertyChanged(nameof(SupplierCount));
            OnPropertyChanged(nameof(LowStockCount));

            StatusText = "Данные обновлены";
        }
        catch (Exception ex)
        {
            StatusText = "Ошибка загрузки";
            await _dialogService.ShowErrorAsync($"Ошибка загрузки данных: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveProductAsync()
    {
        if (!ProductForm.IsValid)
        {
            await _dialogService.ShowErrorAsync("Проверьте корректность полей товара.");
            return;
        }

        try
        {
            IsBusy = true;
            await _inventoryService.SaveProductAsync(ProductForm.ToProduct());
            await LoadAllAsync();
            await _dialogService.ShowInfoAsync("Товар сохранен.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Ошибка сохранения товара: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CreateNewProduct()
    {
        if (Categories.Count == 0 || Units.Count == 0)
        {
            return;
        }

        ProductForm = new ProductFormModel
        {
            IsActive = true,
            CategoryId = Categories[0].CategoryId,
            UnitId = Units[0].UnitId,
            MinStockLevel = 0
        };
        OnPropertyChanged(nameof(ProductForm));
        RaiseCommandStates();
    }

    private async Task DeleteProductAsync()
    {
        if (ProductForm.ProductId <= 0)
        {
            return;
        }

        if (!await _dialogService.ConfirmAsync("Удалить товар?"))
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _inventoryService.DeleteProductAsync(ProductForm.ProductId);
            CreateNewProduct();
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Не удалось удалить товар: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddSupplierAsync()
    {
        var name = await _dialogService.PromptAsync("Введите название поставщика:", "Новый поставщик");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await _inventoryService.AddSupplierAsync(name);
        await LoadAllAsync();
    }

    private async Task AddCategoryAsync()
    {
        var name = await _dialogService.PromptAsync("Введите название категории:", "Новая категория");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await _inventoryService.AddCategoryAsync(name);
        await LoadAllAsync();
    }

    private async Task AddUnitAsync()
    {
        var name = await _dialogService.PromptAsync("Введите название единицы:", "Новая единица");
        var shortName = await _dialogService.PromptAsync("Введите краткое обозначение:", "Новая единица");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(shortName))
        {
            return;
        }

        await _inventoryService.AddUnitAsync(name, shortName);
        await LoadAllAsync();
    }

    private bool CanAddReceiptLine() => LineReceiptProduct is not null && LineReceiptQuantity > 0 && LineReceiptPrice > 0;

    private void AddReceiptLine()
    {
        if (LineReceiptProduct is null)
        {
            return;
        }

        ReceiptLines.Add(new DocumentLineFormModel
        {
            ProductId = LineReceiptProduct.ProductId,
            ProductName = LineReceiptProduct.ProductName,
            Quantity = LineReceiptQuantity,
            Price = LineReceiptPrice
        });
        OnPropertyChanged(nameof(ReceiptTotal));
        RaiseCommandStates();
    }

    private void RemoveReceiptLine()
    {
        if (SelectedReceiptLine is null)
        {
            return;
        }

        ReceiptLines.Remove(SelectedReceiptLine);
        OnPropertyChanged(nameof(ReceiptTotal));
        RaiseCommandStates();
    }

    private async Task CreateReceiptAsync()
    {
        if (SelectedReceiptSupplier is null || ReceiptLines.Count == 0)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var lines = ReceiptLines.Select(x => new DocumentLineRequest { ProductId = x.ProductId, Quantity = x.Quantity, Price = x.Price }).ToList();
            await _inventoryService.CreateReceiptAsync(SelectedReceiptSupplier.SupplierId, lines);
            ReceiptLines.Clear();
            OnPropertyChanged(nameof(ReceiptTotal));
            await LoadAllAsync();
            await _dialogService.ShowInfoAsync("Поступление проведено.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Ошибка поступления: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanAddSaleLine() => LineSaleProduct is not null && LineSaleQuantity > 0 && LineSalePrice > 0;

    private void AddSaleLine()
    {
        if (LineSaleProduct is null)
        {
            return;
        }

        SaleLines.Add(new DocumentLineFormModel
        {
            ProductId = LineSaleProduct.ProductId,
            ProductName = LineSaleProduct.ProductName,
            Quantity = LineSaleQuantity,
            Price = LineSalePrice
        });
        OnPropertyChanged(nameof(SaleTotal));
        RaiseCommandStates();
    }

    private void RemoveSaleLine()
    {
        if (SelectedSaleLine is null)
        {
            return;
        }

        SaleLines.Remove(SelectedSaleLine);
        OnPropertyChanged(nameof(SaleTotal));
        RaiseCommandStates();
    }

    private async Task CreateSaleAsync()
    {
        if (SaleLines.Count == 0)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var lines = SaleLines.Select(x => new DocumentLineRequest { ProductId = x.ProductId, Quantity = x.Quantity, Price = x.Price }).ToList();
            await _inventoryService.CreateSaleAsync(lines);
            SaleLines.Clear();
            OnPropertyChanged(nameof(SaleTotal));
            await LoadAllAsync();
            await _dialogService.ShowInfoAsync("Продажа проведена.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Ошибка продажи: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseCommandStates()
    {
        var commands = new ICommand[]
        {
            LoadCommand, SaveProductCommand, NewProductCommand, DeleteProductCommand,
            AddSupplierCommand, AddCategoryCommand, AddUnitCommand,
            AddReceiptLineCommand, RemoveReceiptLineCommand, CreateReceiptCommand,
            AddSaleLineCommand, RemoveSaleLineCommand, CreateSaleCommand
        };

        foreach (var command in commands)
        {
            switch (command)
            {
                case RelayCommand relay:
                    relay.RaiseCanExecuteChanged();
                    break;
                case AsyncRelayCommand asyncRelay:
                    asyncRelay.RaiseCanExecuteChanged();
                    break;
            }
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
