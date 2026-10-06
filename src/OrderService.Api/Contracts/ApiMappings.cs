using OrderService.Api.Contracts.Responses;
using OrderService.Application.Common.Models;

namespace OrderService.Api.Contracts;

public static class ApiMappings
{
    public static OrderResponse ToResponse(this OrderDto dto) =>
        new(
            dto.Id,
            dto.CustomerId,
            dto.Status,
            dto.Currency,
            dto.Total,
            dto.CreatedAt,
            dto.Items
                .Select(i => new OrderItemResponse(i.ProductId, i.UnitPrice, i.Quantity, i.LineTotal))
                .ToList());

    public static PagedResponse<OrderResponse> ToResponse(this PagedResult<OrderDto> page) =>
        new(
            page.Items.Select(i => i.ToResponse()).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount,
            page.TotalPages);

    public static TokenResponse ToResponse(this Application.Abstractions.TokenIssueResult result) =>
        new(result.AccessToken, result.ExpiresIn, result.TokenType);
}
