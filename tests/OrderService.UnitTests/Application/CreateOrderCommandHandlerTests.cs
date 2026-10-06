using NSubstitute;
using OrderService.Application.Abstractions;
using OrderService.Application.Orders.Commands.CreateOrder;
using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;

namespace OrderService.UnitTests.Application;

public class CreateOrderCommandHandlerTests
{
    private static readonly Guid CustomerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Handle_WhenProductMissing_ShouldThrow()
    {
        var orders = Substitute.For<IOrderRepository>();
        var products = Substitute.For<IProductRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Product>());

        var handler = new CreateOrderCommandHandler(orders, products, uow, clock);

        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            handler.Handle(
                new CreateOrderCommand(CustomerId, "BRL", [new CreateOrderItemRequest(ProductId, 1)]),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCreatePlacedOrder()
    {
        var orders = Substitute.For<IOrderRepository>();
        var products = Substitute.For<IProductRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.Parse("2026-10-05T12:00:00Z"));

        products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new Product(ProductId, "Widget A", 10m, 100)]);

        var handler = new CreateOrderCommandHandler(orders, products, uow, clock);

        var result = await handler.Handle(
            new CreateOrderCommand(CustomerId, "BRL", [new CreateOrderItemRequest(ProductId, 2)]),
            CancellationToken.None);

        Assert.Equal("Placed", result.Status);
        Assert.Equal(20.00m, result.Total);
        await orders.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStockInsufficient_ShouldThrow()
    {
        var orders = Substitute.For<IOrderRepository>();
        var products = Substitute.For<IProductRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new Product(ProductId, "Widget A", 10m, 1)]);

        var handler = new CreateOrderCommandHandler(orders, products, uow, clock);

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            handler.Handle(
                new CreateOrderCommand(CustomerId, "BRL", [new CreateOrderItemRequest(ProductId, 5)]),
                CancellationToken.None));
    }
}
