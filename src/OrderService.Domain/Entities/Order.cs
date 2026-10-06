using OrderService.Domain.Enums;
using OrderService.Domain.Exceptions;

namespace OrderService.Domain.Entities;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public decimal Total { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public bool CanConfirm => Status == OrderStatus.Placed;
    public bool CanCancel => Status is OrderStatus.Placed or OrderStatus.Confirmed;

    private Order()
    {
    }

    public static Order Create(
        Guid customerId,
        string currency,
        IEnumerable<(Guid ProductId, decimal UnitPrice, int Quantity)> items,
        DateTimeOffset createdAt)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer id is required.", nameof(customerId));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));
        }

        var itemList = items?.ToList() ?? [];
        if (itemList.Count == 0)
        {
            throw new EmptyOrderItemsException();
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Placed,
            Currency = currency.Trim().ToUpperInvariant(),
            CreatedAt = createdAt
        };

        foreach (var item in itemList)
        {
            order._items.Add(new OrderItem(order.Id, item.ProductId, item.UnitPrice, item.Quantity));
        }

        order.Total = order._items.Sum(i => i.LineTotal);
        return order;
    }

    public void Confirm()
    {
        if (Status == OrderStatus.Confirmed)
        {
            return;
        }

        if (Status != OrderStatus.Placed)
        {
            throw new InvalidOrderStateException(
                $"Order {Id} cannot be confirmed from status {Status}.");
        }

        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Canceled)
        {
            return;
        }

        if (Status is not (OrderStatus.Placed or OrderStatus.Confirmed))
        {
            throw new InvalidOrderStateException(
                $"Order {Id} cannot be canceled from status {Status}.");
        }

        Status = OrderStatus.Canceled;
    }

    public bool WasConfirmedBeforeCancel(OrderStatus previousStatus) =>
        previousStatus == OrderStatus.Confirmed;
}
