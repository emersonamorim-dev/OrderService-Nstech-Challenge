namespace OrderService.Api.Contracts.Responses;

public sealed record TokenResponse(string AccessToken, int ExpiresIn, string TokenType);

public sealed record OrderItemResponse(Guid ProductId, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    string Currency,
    decimal Total,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
