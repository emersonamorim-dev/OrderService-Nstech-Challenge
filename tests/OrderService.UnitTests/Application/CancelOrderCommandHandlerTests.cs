using NSubstitute;
using OrderService.Application.Abstractions;
using OrderService.Application.Orders.Commands.CancelOrder;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Exceptions;

namespace OrderService.UnitTests.Application;

public class CancelOrderCommandHandlerTests
{
    private static readonly Guid CustomerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Handle_WhenAlreadyCanceled_ShouldBeIdempotentWithoutTouchingStock()
    {
        var order = Order.Create(CustomerId, "BRL", [(ProductId, 10m, 2)], DateTimeOffset.UtcNow);
        order.Cancel();

        var orders = Substitute.For<IOrderRepository>();
        var products = Substitute.For<IProductRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var handler = new CancelOrderCommandHandler(orders, products, uow);
        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal("Canceled", result.Status);
        await products.DidNotReceive()
            .GetTrackedByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPlaced_ShouldCancelWithoutReleasingStock()
    {
        var order = Order.Create(CustomerId, "BRL", [(ProductId, 10m, 2)], DateTimeOffset.UtcNow);
        var product = new Product(ProductId, "Widget A", 10m, 10);

        var orders = Substitute.For<IOrderRepository>();
        var products = Substitute.For<IProductRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var handler = new CancelOrderCommandHandler(orders, products, uow);
        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderStatus.Canceled.ToString(), result.Status);
        Assert.Equal(10, product.AvailableQuantity);
        await products.DidNotReceive()
            .GetTrackedByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenConfirmed_ShouldReleaseStockAndCancel()
    {
        var order = Order.Create(CustomerId, "BRL", [(ProductId, 10m, 2)], DateTimeOffset.UtcNow);
        order.Confirm();
        var product = new Product(ProductId, "Widget A", 10m, 8);

        var orders = Substitute.For<IOrderRepository>();
        var products = Substitute.For<IProductRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        products.GetTrackedByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([product]);
        uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var handler = new CancelOrderCommandHandler(orders, products, uow);
        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderStatus.Canceled.ToString(), result.Status);
        Assert.Equal(10, product.AvailableQuantity);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOrderMissing_ShouldThrow()
    {
        var orderId = Guid.NewGuid();
        var orders = Substitute.For<IOrderRepository>();
        var products = Substitute.For<IProductRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((Order?)null);
        uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var handler = new CancelOrderCommandHandler(orders, products, uow);

        await Assert.ThrowsAsync<OrderNotFoundException>(() =>
            handler.Handle(new CancelOrderCommand(orderId), CancellationToken.None));
    }
}
