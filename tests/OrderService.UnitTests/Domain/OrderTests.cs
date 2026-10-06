using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Exceptions;

namespace OrderService.UnitTests.Domain;

public class OrderTests
{
    private static readonly Guid CustomerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_WhenValidItems_ShouldBePlacedWithCorrectTotal()
    {
        var order = Order.Create(
            CustomerId,
            "brl",
            [(ProductId, 10.00m, 2), (ProductId, 5.00m, 1)],
            DateTimeOffset.UtcNow);

        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal("BRL", order.Currency);
        Assert.Equal(25.00m, order.Total);
        Assert.Equal(2, order.Items.Count);
    }

    [Fact]
    public void Create_WhenNoItems_ShouldThrow()
    {
        Assert.Throws<EmptyOrderItemsException>(() =>
            Order.Create(CustomerId, "BRL", Array.Empty<(Guid, decimal, int)>(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WhenQuantityZero_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Order.Create(CustomerId, "BRL", [(ProductId, 10m, 0)], DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Confirm_WhenPlaced_ShouldBecomeConfirmed()
    {
        var order = CreateSampleOrder();
        order.Confirm();
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldBeIdempotent()
    {
        var order = CreateSampleOrder();
        order.Confirm();
        order.Confirm();
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_WhenCanceled_ShouldThrow()
    {
        var order = CreateSampleOrder();
        order.Cancel();
        Assert.Throws<InvalidOrderStateException>(() => order.Confirm());
    }

    [Fact]
    public void Cancel_WhenPlaced_ShouldBecomeCanceled()
    {
        var order = CreateSampleOrder();
        order.Cancel();
        Assert.Equal(OrderStatus.Canceled, order.Status);
    }

    [Fact]
    public void Cancel_WhenConfirmed_ShouldBecomeCanceled()
    {
        var order = CreateSampleOrder();
        order.Confirm();
        order.Cancel();
        Assert.Equal(OrderStatus.Canceled, order.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyCanceled_ShouldBeIdempotent()
    {
        var order = CreateSampleOrder();
        order.Cancel();
        order.Cancel();
        Assert.Equal(OrderStatus.Canceled, order.Status);
    }

    private static Order CreateSampleOrder() =>
        Order.Create(CustomerId, "BRL", [(ProductId, 10m, 1)], DateTimeOffset.UtcNow);
}
