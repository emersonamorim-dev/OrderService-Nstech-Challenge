using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Persistence.Seed;

namespace OrderService.IntegrationTests;

public sealed class OrderServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"orders-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<OrderDbContext>));
            services.RemoveAll(typeof(OrderDbContext));

            services.AddDbContext<OrderDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}

public class OrdersApiTests : IClassFixture<OrderServiceWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public OrdersApiTests(OrderServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullFlow_CreateConfirmCancel_ShouldBeIdempotent()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var createResponse = await _client.PostAsJsonAsync("/orders", new
        {
            customerId,
            currency = "BRL",
            items = new[]
            {
                new { productId = ProductSeed.WidgetAId, quantity = 2 }
            }
        });

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<OrderApiModel>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Placed", created.Status);
        Assert.Equal(20.00m, created.Total);

        var confirm1 = await _client.PostAsync($"/orders/{created.Id}/confirm", null);
        confirm1.EnsureSuccessStatusCode();
        var confirmed = await confirm1.Content.ReadFromJsonAsync<OrderApiModel>(JsonOptions);
        Assert.Equal("Confirmed", confirmed!.Status);

        var confirm2 = await _client.PostAsync($"/orders/{created.Id}/confirm", null);
        confirm2.EnsureSuccessStatusCode();
        var confirmedAgain = await confirm2.Content.ReadFromJsonAsync<OrderApiModel>(JsonOptions);
        Assert.Equal("Confirmed", confirmedAgain!.Status);

        var get = await _client.GetAsync($"/orders/{created.Id}");
        get.EnsureSuccessStatusCode();

        var cancel1 = await _client.PostAsync($"/orders/{created.Id}/cancel", null);
        cancel1.EnsureSuccessStatusCode();
        var canceled = await cancel1.Content.ReadFromJsonAsync<OrderApiModel>(JsonOptions);
        Assert.Equal("Canceled", canceled!.Status);

        var cancel2 = await _client.PostAsync($"/orders/{created.Id}/cancel", null);
        cancel2.EnsureSuccessStatusCode();
        var canceledAgain = await cancel2.Content.ReadFromJsonAsync<OrderApiModel>(JsonOptions);
        Assert.Equal("Canceled", canceledAgain!.Status);

        var list = await _client.GetAsync($"/orders?customerId={customerId}&page=1&pageSize=10");
        list.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Create_WithoutToken_ShouldReturnUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/orders", new
        {
            customerId = Guid.NewGuid(),
            currency = "BRL",
            items = new[] { new { productId = ProductSeed.WidgetAId, quantity = 1 } }
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenEmptyItems_ShouldReturnBadRequest()
    {
        await AuthorizeAsync();

        var response = await _client.PostAsJsonAsync("/orders", new
        {
            customerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            currency = "BRL",
            items = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenInsufficientStock_ShouldReturnConflict()
    {
        await AuthorizeAsync();

        var response = await _client.PostAsJsonAsync("/orders", new
        {
            customerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            currency = "BRL",
            items = new[] { new { productId = ProductSeed.WidgetCId, quantity = 999 } }
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CancelFromPlaced_ShouldSucceedWithoutPriorConfirm()
    {
        await AuthorizeAsync();

        var createResponse = await _client.PostAsJsonAsync("/orders", new
        {
            customerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            currency = "BRL",
            items = new[] { new { productId = ProductSeed.WidgetBId, quantity = 1 } }
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<OrderApiModel>(JsonOptions);

        var cancel = await _client.PostAsync($"/orders/{created!.Id}/cancel", null);
        cancel.EnsureSuccessStatusCode();
        var canceled = await cancel.Content.ReadFromJsonAsync<OrderApiModel>(JsonOptions);
        Assert.Equal("Canceled", canceled!.Status);
    }

    [Fact]
    public async Task Confirm_WhenCanceled_ShouldReturnConflict()
    {
        await AuthorizeAsync();

        var createResponse = await _client.PostAsJsonAsync("/orders", new
        {
            customerId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            currency = "BRL",
            items = new[] { new { productId = ProductSeed.WidgetAId, quantity = 1 } }
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<OrderApiModel>(JsonOptions);

        var cancel = await _client.PostAsync($"/orders/{created!.Id}/cancel", null);
        cancel.EnsureSuccessStatusCode();

        var confirm = await _client.PostAsync($"/orders/{created.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoints_ShouldReturnSuccess()
    {
        var health = await _client.GetAsync("/health");
        var live = await _client.GetAsync("/health/live");
        var ready = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    private async Task AuthorizeAsync()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<string> GetTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/auth/token", new { username = "demo", password = "demo" });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TokenApiModel>(JsonOptions);
        Assert.NotNull(payload);
        return payload.AccessToken;
    }

    private sealed record TokenApiModel(string AccessToken, int ExpiresIn, string TokenType);

    private sealed record OrderApiModel(
        Guid Id,
        Guid CustomerId,
        string Status,
        string Currency,
        decimal Total,
        DateTimeOffset CreatedAt);
}
