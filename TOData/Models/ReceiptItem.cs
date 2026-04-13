using System;
using System.Collections.Generic;

namespace TOData.Models;

public partial class ReceiptItem
{
    public int ReceiptItemId { get; set; }

    public int ReceiptId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Receipt Receipt { get; set; } = null!;
}
