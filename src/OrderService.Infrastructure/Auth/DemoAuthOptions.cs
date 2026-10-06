namespace OrderService.Infrastructure.Auth;

public sealed class DemoAuthOptions
{
    public const string SectionName = "DemoAuth";

    public string Username { get; set; } = "demo";
    public string Password { get; set; } = "demo";
}
