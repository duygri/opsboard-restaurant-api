using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OpsBoard.Application.Auth;

namespace OpsBoard.Tests.Api;

public sealed class AuditLogApiTests
{
    [Fact]
    public async Task StaffCannotAccessAuditLogs()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var staffClient = await CreateAuthenticatedClientAsync(factory, "staff@opsboard.local", "Staff123!");

        var response = await staffClient.GetAsync("/api/audit-logs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminCanAccessAuditLogs()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var adminClient = await CreateAuthenticatedClientAsync(factory, "admin@opsboard.local", "Admin123!");

        var response = await adminClient.GetAsync("/api/audit-logs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
}
