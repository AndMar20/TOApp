using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TOData.Models;

public partial class TodbContext : DbContext
{
    public TodbContext()
    {
    }

    public TodbContext(DbContextOptions<TodbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Receipt> Receipts { get; set; }

    public virtual DbSet<ReceiptItem> ReceiptItems { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<SaleItem> SaleItems { get; set; }

    public virtual DbSet<StockBalance> StockBalances { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=Andris;Initial Catalog=todb;User ID=sa;Password=sa;Encrypt=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0B7D35F361");

            entity.HasIndex(e => e.CategoryName, "UQ__Categori__8517B2E0A00A9A70").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(100);

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .HasForeignKey(d => d.ParentCategoryId)
                .HasConstraintName("FK_Categories_ParentCategory");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6CDC946B780");

            entity.HasIndex(e => e.Sku, "UQ__Products__CA1ECF0D95272560").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Products_IsActive");
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .HasColumnName("SKU");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Category");

            entity.HasOne(d => d.Unit).WithMany(p => p.Products)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Unit");
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId).HasName("PK__Receipts__CC08C420337C38F6");

            entity.Property(e => e.ReceiptDate)
                .HasDefaultValueSql("(getdate())", "DF_Receipts_ReceiptDate")
                .HasColumnType("datetime");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Receipts)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Receipts_Supplier");
        });

        modelBuilder.Entity<ReceiptItem>(entity =>
        {
            entity.HasKey(e => e.ReceiptItemId).HasName("PK__ReceiptI__AF7BE10DB895E2AC");

            entity.HasIndex(e => new { e.ReceiptId, e.ProductId }, "UQ_ReceiptItems").IsUnique();

            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.ReceiptItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReceiptItems_Product");

            entity.HasOne(d => d.Receipt).WithMany(p => p.ReceiptItems)
                .HasForeignKey(d => d.ReceiptId)
                .HasConstraintName("FK_ReceiptItems_Receipt");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.SaleId).HasName("PK__Sales__1EE3C3FF2F61C81A");

            entity.Property(e => e.SaleDate)
                .HasDefaultValueSql("(getdate())", "DF_Sales_SaleDate")
                .HasColumnType("datetime");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasKey(e => e.SaleItemId).HasName("PK__SaleItem__C605940121E23A70");

            entity.HasIndex(e => new { e.SaleId, e.ProductId }, "UQ_SaleItems").IsUnique();

            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.SaleItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SaleItems_Product");

            entity.HasOne(d => d.Sale).WithMany(p => p.SaleItems)
                .HasForeignKey(d => d.SaleId)
                .HasConstraintName("FK_SaleItems_Sale");
        });

        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("StockBalance");

            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .HasColumnName("SKU");
            entity.Property(e => e.UnitShortName).HasMaxLength(20);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE666B456A055C1");

            entity.HasIndex(e => e.SupplierName, "UQ__Supplier__9C5DF66FEE4BD84B").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.SupplierName).HasMaxLength(200);
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.UnitId).HasName("PK__Units__44F5ECB5F5364E7C");

            entity.HasIndex(e => e.UnitShortName, "UQ__Units__3CA9E6AA67C69DD9").IsUnique();

            entity.HasIndex(e => e.UnitName, "UQ__Units__B5EE6678E49F938C").IsUnique();

            entity.Property(e => e.UnitName).HasMaxLength(100);
            entity.Property(e => e.UnitShortName).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
