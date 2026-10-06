using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Api.Contracts;
using OrderService.Api.Contracts.Requests;
using OrderService.Api.Contracts.Responses;
using OrderService.Application.Orders.Commands.CancelOrder;
using OrderService.Application.Orders.Commands.ConfirmOrder;
using OrderService.Application.Orders.Commands.CreateOrder;
using OrderService.Application.Orders.Queries.GetOrderById;
using OrderService.Application.Orders.Queries.ListOrders;
using OrderService.Domain.Enums;

namespace OrderService.Api.Controllers;

[ApiController]
[Authorize]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.CustomerId,
            request.Currency,
            request.Items
                .Select(i => new Application.Orders.Commands.CreateOrder.CreateOrderItemRequest(i.ProductId, i.Quantity))
                .ToList());

        var order = await _mediator.Send(command, cancellationToken);
        var response = order.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> Confirm(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _mediator.Send(new ConfirmOrderCommand(id), cancellationToken);
        return Ok(order.ToResponse());
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _mediator.Send(new CancelOrderCommand(id), cancellationToken);
        return Ok(order.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        return Ok(order.ToResponse());
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<OrderResponse>>> List(
        [FromQuery] ListOrdersRequest request,
        CancellationToken cancellationToken)
    {
        OrderStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var parsed))
            {
                return ValidationProblem(detail: $"Invalid status '{request.Status}'.");
            }

            status = parsed;
        }

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

        var result = await _mediator.Send(
            new ListOrdersQuery(
                request.CustomerId,
                status,
                request.From,
                request.To,
                page,
                pageSize),
            cancellationToken);

        return Ok(result.ToResponse());
    }
}
