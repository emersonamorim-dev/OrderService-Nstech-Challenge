using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Persistence.Seed;

public static class ProductSeed
{
    public static readonly Guid WidgetAId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid WidgetBId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid WidgetCId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static async Task EnsureSeedAsync(OrderDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Products.AddRange(
            new Product(WidgetAId, "Widget A", 10.00m, 100),
            new Product(WidgetBId, "Widget B", 25.50m, 50),
            new Product(WidgetCId, "Widget C", 99.90m, 5));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
