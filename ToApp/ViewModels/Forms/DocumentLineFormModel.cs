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
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(Amount));
            }
        }
    }

    public decimal Price
    {
        get => _price;
        set
        {
            if (SetProperty(ref _price, value))
            {
                OnPropertyChanged(nameof(Amount));
            }
        }
    }

    public decimal Amount => Quantity * Price;

    public string Error => string.Empty;

    public string this[string columnName] => columnName switch
    {
        nameof(ProductId) => ProductId <= 0 ? "Выберите товар." : string.Empty,
        nameof(Quantity) => Quantity <= 0 ? "Количество должно быть больше нуля." : string.Empty,
        nameof(Price) => Price <= 0 ? "Цена должна быть больше нуля." : string.Empty,
        _ => string.Empty
    };
}
