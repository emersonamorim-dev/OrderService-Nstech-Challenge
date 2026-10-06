namespace OrderService.Domain.Exceptions;

public sealed class OrderNotFoundException : DomainException
{
    public Guid OrderId { get; }

    public OrderNotFoundException(Guid orderId)
        : base($"Order {orderId} was not found.")
    {
        OrderId = orderId;
    }
}
