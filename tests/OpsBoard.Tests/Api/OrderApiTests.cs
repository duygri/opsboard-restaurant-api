using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpsBoard.Application.Auth;
using OpsBoard.Application.Orders;
using OpsBoard.Domain.Orders;
using OpsBoard.Domain.Tables;
using OpsBoard.Infrastructure.Persistence;

namespace OpsBoard.Tests.Api;

public sealed class OrderApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task CreateOrder_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(Guid.NewGuid(), Array.Empty<CreateOrderItemRequest>()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StaffCanCreateOrder()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var client = await CreateStaffClientAsync(factory);
        var (tableId, menuItemId) = await GetSeedIdsAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(tableId, new[] { new CreateOrderItemRequest(menuItemId, 2) }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(OrderStatus.New, body.Status);
        Assert.NotEmpty(body.Items);
    }

    [Fact]
    public async Task StaffCannotCreateEmptyOrder()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var client = await CreateStaffClientAsync(factory);
        var (tableId, _) = await GetSeedIdsAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(tableId, Array.Empty<CreateOrderItemRequest>()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StaffCannotCreateOrderForOccupiedTable()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var client = await CreateStaffClientAsync(factory);
        var (tableId, menuItemId) = await GetSeedIdsAsync(factory);

        var first = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(tableId, new[] { new CreateOrderItemRequest(menuItemId, 1) }));
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(tableId, new[] { new CreateOrderItemRequest(menuItemId, 1) }));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task StaffCanUpdateOrderThroughValidTransition()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var client = await CreateStaffClientAsync(factory);
        var (tableId, menuItemId) = await GetSeedIdsAsync(factory);
        var created = await CreateOrderAsync(client, tableId, menuItemId);

        var response = await client.PatchAsJsonAsync(
            $"/api/orders/{created.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Preparing));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(OrderStatus.Preparing, body.Status);
    }

    [Fact]
    public async Task InvalidTransitionReturnsConflict()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var client = await CreateStaffClientAsync(factory);
        var (tableId, menuItemId) = await GetSeedIdsAsync(factory);
        var created = await CreateOrderAsync(client, tableId, menuItemId);

        var response = await client.PatchAsJsonAsync(
            $"/api/orders/{created.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Ready));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<HttpClient> CreateStaffClientAsync(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("staff@opsboard.local", "Staff123!"));
        loginResponse.EnsureSuccessStatusCode();
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private static async Task<(Guid TableId, Guid MenuItemId)> GetSeedIdsAsync(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsBoardDbContext>();
        var tableId = await dbContext.RestaurantTables
            .Where(table => table.Status == TableStatus.Available)
            .OrderBy(table => table.Name)
            .Select(table => table.Id)
            .FirstAsync();
        var menuItemId = await dbContext.MenuItems
            .Where(item => item.IsAvailable)
            .OrderBy(item => item.Name)
            .Select(item => item.Id)
            .FirstAsync();
        return (tableId, menuItemId);
    }

    private static async Task<OrderDetailResponse> CreateOrderAsync(HttpClient client, Guid tableId, Guid menuItemId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(tableId, new[] { new CreateOrderItemRequest(menuItemId, 1) }));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions))!;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
