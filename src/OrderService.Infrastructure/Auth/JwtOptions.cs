namespace OrderService.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "OrderService";
    public string Audience { get; set; } = "OrderService";
    public string Secret { get; set; } = "SuperSecretKeyForOrderServiceDevOnly_ChangeMe_32chars!";
    public int ExpirationMinutes { get; set; } = 60;
}
