using Microsoft.Extensions.Options;
using OrderService.Application.Abstractions;

namespace OrderService.Infrastructure.Auth;

public sealed class DemoAuthCredentialValidator : IAuthCredentialValidator
{
    private readonly DemoAuthOptions _options;

    public DemoAuthCredentialValidator(IOptions<DemoAuthOptions> options)
    {
        _options = options.Value;
    }

    public bool Validate(string username, string password) =>
        string.Equals(username, _options.Username, StringComparison.Ordinal) &&
        string.Equals(password, _options.Password, StringComparison.Ordinal);
}
