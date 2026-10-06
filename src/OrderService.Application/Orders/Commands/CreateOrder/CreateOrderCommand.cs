using FluentValidation;
using MediatR;
using OrderService.Application.Abstractions;
using OrderService.Application.Common.Models;
using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;

namespace OrderService.Application.Orders.Commands.CreateOrder;

public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);

public sealed record CreateOrderCommand(
    Guid CustomerId,
    string Currency,
    IReadOnlyList<CreateOrderItemRequest> Items) : IRequest<OrderDto>;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productsById = products.ToDictionary(p => p.Id);

        foreach (var productId in productIds)
        {
            if (!productsById.ContainsKey(productId))
            {
                throw new ProductNotFoundException(productId);
            }
        }

        // Fail-fast stock pre-check (reservation happens on Confirm)
        var requestedByProduct = request.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        foreach (var (productId, quantity) in requestedByProduct)
        {
            var product = productsById[productId];
            if (!product.HasAvailability(quantity))
            {
                throw new InsufficientStockException(productId, quantity, product.AvailableQuantity);
            }
        }

        var orderItems = request.Items
            .Select(i =>
            {
                var product = productsById[i.ProductId];
                return (i.ProductId, product.UnitPrice, i.Quantity);
            })
            .ToList();

        var order = Order.Create(
            request.CustomerId,
            request.Currency,
            orderItems,
            _dateTimeProvider.UtcNow);

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
