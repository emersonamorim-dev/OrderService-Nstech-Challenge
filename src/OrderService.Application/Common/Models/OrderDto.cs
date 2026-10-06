using OrderService.Domain.Entities;
using OrderService.Domain.Enums;

namespace OrderService.Application.Common.Models;

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    string Currency,
    decimal Total,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderItemDto(
    Guid ProductId,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public static class OrderMappings
{
    public static OrderDto ToDto(this Order order) =>
        new(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.Currency,
            order.Total,
            order.CreatedAt,
            order.Items
                .Select(i => new OrderItemDto(i.ProductId, i.UnitPrice, i.Quantity, i.LineTotal))
                .ToList());
}
