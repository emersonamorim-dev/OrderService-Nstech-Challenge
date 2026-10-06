using OrderService.Domain.Entities;
using OrderService.Domain.Enums;

namespace OrderService.Application.Abstractions;

public sealed record OrderListFilter(
    Guid? CustomerId,
    OrderStatus? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize);

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        OrderListFilter filter,
        CancellationToken cancellationToken);
}
