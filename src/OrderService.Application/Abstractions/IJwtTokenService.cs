namespace OrderService.Application.Abstractions;

public interface IJwtTokenService
{
    TokenIssueResult IssueToken(string username);
}

public sealed record TokenIssueResult(string AccessToken, int ExpiresIn, string TokenType);
