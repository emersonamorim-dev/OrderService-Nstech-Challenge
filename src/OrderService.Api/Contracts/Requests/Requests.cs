namespace OrderService.Api.Contracts.Requests;

public sealed class IssueTokenRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class CreateOrderRequest
{
    public Guid CustomerId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public sealed class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public sealed class ListOrdersRequest
{
    public Guid? CustomerId { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
