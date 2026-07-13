using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpsBoard.Application.Auth;
using OpsBoard.Domain.Users;
using OpsBoard.Infrastructure.Persistence;

namespace OpsBoard.Tests.Api;

public sealed class AuthApiTests
{
    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsToken()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin@opsboard.local", "Admin123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.Equal(UserRole.Admin, body.User.Role);
    }

    [Fact]
    public async Task Login_WithBadPassword_ReturnsUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin@opsboard.local", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithStaffToken_ReturnsStaffUser()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();
        var login = await LoginAsync(client, "staff@opsboard.local", "Staff123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.NotNull(body);
        Assert.Equal("staff@opsboard.local", body.Email);
        Assert.Equal(UserRole.Staff, body.Role);
    }

    [Fact]
    public async Task AuthorizedEndpoint_WithDisabledUserToken_ReturnsUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();
        var login = await LoginAsync(client, "staff@opsboard.local", "Staff123!");
        await DisableUserAsync(factory, "staff@opsboard.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.GetAsync("/api/tables");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<LoginResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    private static async Task DisableUserAsync(CustomWebApplicationFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsBoardDbContext>();
        var user = await dbContext.Users.FirstAsync(candidate => candidate.Email == email);
        typeof(AppUser).GetProperty(nameof(AppUser.IsActive))!.SetValue(user, false);
        await dbContext.SaveChangesAsync();
    }
}
