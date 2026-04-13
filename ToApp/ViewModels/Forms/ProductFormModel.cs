using System.ComponentModel;
using TOData.Models;
using ToApp.Infrastructure;

namespace ToApp.ViewModels.Forms;

public sealed class ProductFormModel : ObservableObject, IDataErrorInfo
{
    private int _productId;
    private string _sku = string.Empty;
    private string _productName = string.Empty;
    private int _categoryId;
    private int _unitId;
    private int _minStockLevel;
    private string? _description;
    private bool _isActive = true;

    public int ProductId
    {
        get => _productId;
        set => SetProperty(ref _productId, value);
    }

    public string Sku
    {
        get => _sku;
        set => SetProperty(ref _sku, value);
    }

    public string ProductName
    {
        get => _productName;
        set => SetProperty(ref _productName, value);
    }

    public int CategoryId
    {
        get => _categoryId;
        set => SetProperty(ref _categoryId, value);
    }

    public int UnitId
    {
        get => _unitId;
        set => SetProperty(ref _unitId, value);
    }

    public int MinStockLevel
    {
        get => _minStockLevel;
        set => SetProperty(ref _minStockLevel, value);
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string Error => string.Empty;

    public string this[string columnName] => columnName switch
    {
        nameof(Sku) when string.IsNullOrWhiteSpace(Sku) => "Артикул обязателен.",
        nameof(ProductName) when string.IsNullOrWhiteSpace(ProductName) => "Наименование обязательно.",
        nameof(CategoryId) when CategoryId <= 0 => "Выберите категорию.",
        nameof(UnitId) when UnitId <= 0 => "Выберите единицу измерения.",
        nameof(MinStockLevel) when MinStockLevel < 0 => "Мин. остаток не может быть отрицательным.",
        _ => string.Empty
    };

    public bool IsValid => string.IsNullOrWhiteSpace(this[nameof(Sku)])
                           && string.IsNullOrWhiteSpace(this[nameof(ProductName)])
                           && string.IsNullOrWhiteSpace(this[nameof(CategoryId)])
                           && string.IsNullOrWhiteSpace(this[nameof(UnitId)])
                           && string.IsNullOrWhiteSpace(this[nameof(MinStockLevel)]);

    public static ProductFormModel FromProduct(Product product)
    {
        return new ProductFormModel
        {
            ProductId = product.ProductId,
            Sku = product.Sku,
            ProductName = product.ProductName,
            CategoryId = product.CategoryId,
            UnitId = product.UnitId,
            MinStockLevel = product.MinStockLevel,
            Description = product.Description,
            IsActive = product.IsActive
        };
    }

    public Product ToProduct()
    {
        return new Product
        {
            ProductId = ProductId,
            Sku = Sku,
            ProductName = ProductName,
            CategoryId = CategoryId,
            UnitId = UnitId,
            MinStockLevel = MinStockLevel,
            Description = Description,
            IsActive = IsActive
        };
    }
}
