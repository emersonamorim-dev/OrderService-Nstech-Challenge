using FluentValidation;
using MediatR;
using OrderService.Application.Abstractions;
using OrderService.Application.Common.Models;
using OrderService.Domain.Enums;

namespace OrderService.Application.Orders.Queries.ListOrders;

public sealed record ListOrdersQuery(
    Guid? CustomerId,
    OrderStatus? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize) : IRequest<PagedResult<OrderDto>>;

public sealed class ListOrdersQueryValidator : AbstractValidator<ListOrdersQuery>
{
    public const int MaxPageSize = 100;

    public ListOrdersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("'from' must be less than or equal to 'to'.");
    }
}

public sealed class ListOrdersQueryHandler : IRequestHandler<ListOrdersQuery, PagedResult<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public ListOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<PagedResult<OrderDto>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        var filter = new OrderListFilter(
            request.CustomerId,
            request.Status,
            request.From,
            request.To,
            request.Page,
            request.PageSize);

        var (items, totalCount) = await _orderRepository.ListAsync(filter, cancellationToken);

        return new PagedResult<OrderDto>(
            items.Select(o => o.ToDto()).ToList(),
            request.Page,
            request.PageSize,
            totalCount);
    }
}
