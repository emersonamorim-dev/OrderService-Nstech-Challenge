namespace OrderService.Domain.Exceptions;

public sealed class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string message = "A concurrency conflict occurred while updating stock or order state.")
        : base(message)
    {
    }
}
