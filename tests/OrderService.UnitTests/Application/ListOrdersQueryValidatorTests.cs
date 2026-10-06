using FluentValidation;
using OrderService.Application.Orders.Queries.ListOrders;
using OrderService.Domain.Enums;

namespace OrderService.UnitTests.Application;

public class ListOrdersQueryValidatorTests
{
    private readonly ListOrdersQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenPageInvalid_ShouldFail()
    {
        var result = _validator.Validate(new ListOrdersQuery(null, null, null, null, 0, 20));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPageSizeAboveMax_ShouldFail()
    {
        var result = _validator.Validate(
            new ListOrdersQuery(null, OrderStatus.Placed, null, null, 1, ListOrdersQueryValidator.MaxPageSize + 1));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenFromAfterTo_ShouldFail()
    {
        var from = DateTimeOffset.Parse("2026-10-05T12:00:00Z");
        var to = DateTimeOffset.Parse("2026-10-01T12:00:00Z");
        var result = _validator.Validate(new ListOrdersQuery(null, null, from, to, 1, 20));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        var result = _validator.Validate(
            new ListOrdersQuery(Guid.NewGuid(), OrderStatus.Placed, null, null, 1, 20));
        Assert.True(result.IsValid);
    }
}
