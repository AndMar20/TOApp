using System;
using System.Collections.Generic;

namespace TOData.Models;

public partial class Receipt
{
    public int ReceiptId { get; set; }

    public int SupplierId { get; set; }

    public DateTime ReceiptDate { get; set; }

    public decimal TotalAmount { get; set; }

    public virtual ICollection<ReceiptItem> ReceiptItems { get; set; } = new List<ReceiptItem>();

    public virtual Supplier Supplier { get; set; } = null!;
}
