using FluentValidation;
using MediatR;
using OrderService.Application.Abstractions;
using OrderService.Application.Common.Models;
using OrderService.Domain.Enums;
using OrderService.Domain.Exceptions;

namespace OrderService.Application.Orders.Commands.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId) : IRequest<OrderDto>;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        OrderDto? result = null;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, ct)
                ?? throw new OrderNotFoundException(request.OrderId);

            // Idempotent: already canceled → same result
            if (order.Status == OrderStatus.Canceled)
            {
                result = order.ToDto();
                return;
            }

            if (!order.CanCancel)
            {
                throw new InvalidOrderStateException(
                    $"Order {order.Id} cannot be canceled from status {order.Status}.");
            }

            var previousStatus = order.Status;

            if (previousStatus == OrderStatus.Confirmed)
            {
                var productIds = order.Items.Select(i => i.ProductId).Distinct();
                var products = await _productRepository.GetTrackedByIdsAsync(productIds, ct);
                var productsById = products.ToDictionary(p => p.Id);

                foreach (var item in order.Items)
                {
                    if (!productsById.TryGetValue(item.ProductId, out var product))
                    {
                        throw new ProductNotFoundException(item.ProductId);
                    }

                    product.Release(item.Quantity);
                }
            }

            order.Cancel();
            await _unitOfWork.SaveChangesAsync(ct);
            result = order.ToDto();
        }, cancellationToken);

        return result!;
    }
}
