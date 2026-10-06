using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(o => o.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(o => o.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(o => o.Total).HasColumnName("total").HasPrecision(18, 2).IsRequired();
        builder.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(o => o.CustomerId).HasDatabaseName("ix_orders_customer_id");
        builder.HasIndex(o => o.Status).HasDatabaseName("ix_orders_status");
        builder.HasIndex(o => o.CreatedAt).HasDatabaseName("ix_orders_created_at");
        builder.HasIndex(o => new { o.CustomerId, o.Status, o.CreatedAt })
            .HasDatabaseName("ix_orders_customer_status_created");
    }
}
