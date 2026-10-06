namespace OrderService.Domain.Exceptions;

public sealed class EmptyOrderItemsException : DomainException
{
    public EmptyOrderItemsException()
        : base("Order must contain at least one item.")
    {
    }
}
