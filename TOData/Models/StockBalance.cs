using System;
using System.Collections.Generic;

namespace TOData.Models;

public partial class StockBalance
{
    public int ProductId { get; set; }

    public string Sku { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string UnitShortName { get; set; } = null!;

    public int MinStockLevel { get; set; }

    public int? CurrentStock { get; set; }
}
