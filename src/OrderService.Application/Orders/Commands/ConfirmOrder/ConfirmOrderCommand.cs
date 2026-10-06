using FluentValidation;
using MediatR;
using OrderService.Application.Abstractions;
using OrderService.Application.Common.Models;
using OrderService.Domain.Enums;
using OrderService.Domain.Exceptions;

namespace OrderService.Application.Orders.Commands.ConfirmOrder;

public sealed record ConfirmOrderCommand(Guid OrderId) : IRequest<OrderDto>;

public sealed class ConfirmOrderCommandValidator : AbstractValidator<ConfirmOrderCommand>
{
    public ConfirmOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

public sealed class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        OrderDto? result = null;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, ct)
                ?? throw new OrderNotFoundException(request.OrderId);

            // Idempotent: already confirmed → same result, no stock mutation
            if (order.Status == OrderStatus.Confirmed)
            {
                result = order.ToDto();
                return;
            }

            if (!order.CanConfirm)
            {
                throw new InvalidOrderStateException(
                    $"Order {order.Id} cannot be confirmed from status {order.Status}.");
            }

            var productIds = order.Items.Select(i => i.ProductId).Distinct();
            var products = await _productRepository.GetTrackedByIdsAsync(productIds, ct);
            var productsById = products.ToDictionary(p => p.Id);

            foreach (var item in order.Items)
            {
                if (!productsById.TryGetValue(item.ProductId, out var product))
                {
                    throw new ProductNotFoundException(item.ProductId);
                }

                product.Reserve(item.Quantity);
            }

            order.Confirm();
            await _unitOfWork.SaveChangesAsync(ct);
            result = order.ToDto();
        }, cancellationToken);

        return result!;
    }
}
