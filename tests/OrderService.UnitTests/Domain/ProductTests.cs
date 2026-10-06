using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;

namespace OrderService.UnitTests.Domain;

public class ProductTests
{
    private static readonly Guid ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Reserve_WhenEnoughStock_ShouldDecreaseAvailability()
    {
        var product = new Product(ProductId, "Widget A", 10m, 5);
        product.Reserve(3);
        Assert.Equal(2, product.AvailableQuantity);
        Assert.Equal(1, product.Version);
    }

    [Fact]
    public void Reserve_WhenInsufficientStock_ShouldThrow()
    {
        var product = new Product(ProductId, "Widget A", 10m, 2);
        var ex = Assert.Throws<InsufficientStockException>(() => product.Reserve(3));
        Assert.Equal(ProductId, ex.ProductId);
        Assert.Equal(2, product.AvailableQuantity);
    }

    [Fact]
    public void Release_ShouldIncreaseAvailabilityAndBumpVersion()
    {
        var product = new Product(ProductId, "Widget A", 10m, 2);
        product.Release(3);
        Assert.Equal(5, product.AvailableQuantity);
        Assert.Equal(1, product.Version);
    }

    [Fact]
    public void HasAvailability_ShouldReflectStock()
    {
        var product = new Product(ProductId, "Widget A", 10m, 2);
        Assert.True(product.HasAvailability(2));
        Assert.False(product.HasAvailability(3));
    }
}
