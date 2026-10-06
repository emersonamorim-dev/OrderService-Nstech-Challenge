namespace OrderService.Domain.Exceptions;

public sealed class InsufficientStockException : DomainException
{
    public Guid ProductId { get; }
    public int Requested { get; }
    public int Available { get; }

    public InsufficientStockException(Guid productId, int requested, int available)
        : base($"Product {productId} has insufficient stock. Requested: {requested}, Available: {available}.")
    {
        ProductId = productId;
        Requested = requested;
        Available = available;
    }
}
