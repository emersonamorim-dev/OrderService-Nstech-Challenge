using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(i => i.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(i => i.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.Quantity).HasColumnName("quantity").IsRequired();

        builder.Ignore(i => i.LineTotal);

        builder.HasIndex(i => i.OrderId).HasDatabaseName("ix_order_items_order_id");
        builder.HasIndex(i => i.ProductId).HasDatabaseName("ix_order_items_product_id");
    }
}
