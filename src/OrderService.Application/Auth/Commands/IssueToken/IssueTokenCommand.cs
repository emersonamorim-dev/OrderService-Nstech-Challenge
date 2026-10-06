using FluentValidation;
using MediatR;
using OrderService.Application.Abstractions;

namespace OrderService.Application.Auth.Commands.IssueToken;

public sealed record IssueTokenCommand(string Username, string Password) : IRequest<TokenIssueResult>;

public sealed class IssueTokenCommandValidator : AbstractValidator<IssueTokenCommand>
{
    public IssueTokenCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class IssueTokenCommandHandler : IRequestHandler<IssueTokenCommand, TokenIssueResult>
{
    private readonly IAuthCredentialValidator _credentialValidator;
    private readonly IJwtTokenService _jwtTokenService;

    public IssueTokenCommandHandler(
        IAuthCredentialValidator credentialValidator,
        IJwtTokenService jwtTokenService)
    {
        _credentialValidator = credentialValidator;
        _jwtTokenService = jwtTokenService;
    }

    public Task<TokenIssueResult> Handle(IssueTokenCommand request, CancellationToken cancellationToken)
    {
        if (!_credentialValidator.Validate(request.Username, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var result = _jwtTokenService.IssueToken(request.Username);
        return Task.FromResult(result);
    }
}
