using System.ComponentModel;
using TOData.Models;
using ToApp.Infrastructure;

namespace ToApp.ViewModels.Forms;

public sealed class ProductFormModel : ObservableObject, IDataErrorInfo
{
    private string _sku = string.Empty;
    private string _productName = string.Empty;
    private int _categoryId;
    private int _unitId;
    private int _minStockLevel;
    private string? _description;
    private bool _isActive = true;

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
        nameof(Sku) => string.IsNullOrWhiteSpace(Sku) ? "Укажите артикул." : string.Empty,
        nameof(ProductName) => string.IsNullOrWhiteSpace(ProductName) ? "Укажите наименование товара." : string.Empty,
        nameof(CategoryId) => CategoryId <= 0 ? "Выберите категорию." : string.Empty,
        nameof(UnitId) => UnitId <= 0 ? "Выберите единицу измерения." : string.Empty,
        nameof(MinStockLevel) => MinStockLevel < 0 ? "Мин. остаток не может быть отрицательным." : string.Empty,
        _ => string.Empty
    };

    public static ProductFormModel FromProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new ProductFormModel
        {
            Sku = product.Sku,
            ProductName = product.ProductName,
            CategoryId = product.CategoryId,
            UnitId = product.UnitId,
            MinStockLevel = product.MinStockLevel,
            Description = product.Description,
            IsActive = product.IsActive
        };
    }
}
