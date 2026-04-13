using System.ComponentModel;
using ToApp.Infrastructure;

namespace ToApp.ViewModels.Forms;

public sealed class DocumentLineFormModel : ObservableObject, IDataErrorInfo
{
    private int _productId;
    private string _productName = string.Empty;
    private int _quantity = 1;
    private decimal _price;

    public int ProductId
    {
        get => _productId;
        set => SetProperty(ref _productId, value);
    }

    public string ProductName
    {
        get => _productName;
        set => SetProperty(ref _productName, value);
    }

    public int Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, value);
    }

    public decimal Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    public decimal Amount => Quantity * Price;

    public string Error => string.Empty;

    public string this[string columnName] => columnName switch
    {
        nameof(ProductId) when ProductId <= 0 => "Выберите товар.",
        nameof(Quantity) when Quantity <= 0 => "Количество должно быть больше 0.",
        nameof(Price) when Price <= 0 => "Цена должна быть больше 0.",
        _ => string.Empty
    };

    public bool IsValid => string.IsNullOrWhiteSpace(this[nameof(ProductId)])
                           && string.IsNullOrWhiteSpace(this[nameof(Quantity)])
                           && string.IsNullOrWhiteSpace(this[nameof(Price)]);
}
