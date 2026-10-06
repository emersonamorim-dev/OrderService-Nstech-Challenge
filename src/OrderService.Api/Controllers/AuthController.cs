using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Api.Contracts;
using OrderService.Api.Contracts.Requests;
using OrderService.Api.Contracts.Responses;
using OrderService.Application.Auth.Commands.IssueToken;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> IssueToken(
        [FromBody] IssueTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new IssueTokenCommand(request.Username, request.Password),
            cancellationToken);

        return Ok(result.ToResponse());
    }
}
