using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpsBoard.Application.Auth;
using OpsBoard.Application.Orders;
using OpsBoard.Application.Reports;
using OpsBoard.Domain.Orders;
using OpsBoard.Domain.Tables;
using OpsBoard.Infrastructure.Persistence;

namespace OpsBoard.Tests.Api;

public sealed class ReportsApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task StaffCannotAccessDailyReport()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var staffClient = await CreateAuthenticatedClientAsync(factory, "staff@opsboard.local", "Staff123!");
        var reportDate = TodayInHoChiMinh();

        var response = await staffClient.GetAsync($"/api/reports/daily?date={reportDate:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminCanAccessDailyReport()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var adminClient = await CreateAuthenticatedClientAsync(factory, "admin@opsboard.local", "Admin123!");
        var reportDate = TodayInHoChiMinh();

        var response = await adminClient.GetAsync($"/api/reports/daily?date={reportDate:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DailyReportIncludesPaidOrdersAndExcludesNonPaidRevenue()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var staffClient = await CreateAuthenticatedClientAsync(factory, "staff@opsboard.local", "Staff123!");
        var adminClient = await CreateAuthenticatedClientAsync(factory, "admin@opsboard.local", "Admin123!");
        var (firstTableId, secondTableId, menuItemId) = await GetSeedIdsAsync(factory);
        var paidOrder = await CreateOrderAsync(staffClient, firstTableId, menuItemId);
        await MoveToPaidAsync(staffClient, paidOrder.Id);
        await CreateOrderAsync(staffClient, secondTableId, menuItemId);
        var reportDate = TodayInHoChiMinh();

        var response = await adminClient.GetAsync($"/api/reports/daily?date={reportDate:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DailyReportResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(reportDate, body.Date);
        Assert.Equal("Asia/Ho_Chi_Minh", body.TimeZone);
        Assert.Equal(paidOrder.Total, body.Revenue);
        Assert.Equal(1, body.PaidOrderCount);
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(
        CustomWebApplicationFactory factory,
        string email,
        string password)
    {
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private static async Task<(Guid FirstTableId, Guid SecondTableId, Guid MenuItemId)> GetSeedIdsAsync(
        CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsBoardDbContext>();
        var tableIds = await dbContext.RestaurantTables
            .Where(table => table.Status == TableStatus.Available)
            .OrderBy(table => table.Name)
            .Select(table => table.Id)
            .Take(2)
            .ToArrayAsync();
        var menuItemId = await dbContext.MenuItems
            .Where(item => item.IsAvailable)
            .OrderBy(item => item.Name)
            .Select(item => item.Id)
            .FirstAsync();
        return (tableIds[0], tableIds[1], menuItemId);
    }

    private static async Task<OrderDetailResponse> CreateOrderAsync(HttpClient client, Guid tableId, Guid menuItemId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(tableId, new[] { new CreateOrderItemRequest(menuItemId, 1) }));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions))!;
    }

    private static async Task MoveToPaidAsync(HttpClient client, Guid orderId)
    {
        foreach (var status in new[] { OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.Served, OrderStatus.Paid })
        {
            var response = await client.PatchAsJsonAsync(
                $"/api/orders/{orderId}/status",
                new UpdateOrderStatusRequest(status));
            response.EnsureSuccessStatusCode();
        }
    }

    private static DateOnly TodayInHoChiMinh()
    {
        return DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
