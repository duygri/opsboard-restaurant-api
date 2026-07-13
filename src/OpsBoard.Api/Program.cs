using Microsoft.EntityFrameworkCore;
using OpsBoard.Application.Abstractions;
using OpsBoard.Infrastructure.Auth;
using OpsBoard.Infrastructure.Persistence;
using OpsBoard.Infrastructure.Persistence.Seed;
using OpsBoard.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=opsboard;Username=postgres;Password=postgres";

builder.Services.AddDbContext<OpsBoardDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<ISystemClock, SystemClock>();
builder.Services.AddScoped<DemoDataSeeder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await ApplyMigrationsAndSeedAsync(app);
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

static async Task ApplyMigrationsAndSeedAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OpsBoardDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();

    try
    {
        await dbContext.Database.MigrateAsync();
        await seeder.SeedAsync();
    }
    catch (Exception exception)
    {
        throw new InvalidOperationException(
            "Failed to migrate or seed the OpsBoard database. Check the DefaultConnection string and PostgreSQL availability.",
            exception);
    }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
