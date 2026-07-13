using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpsBoard.Api.Auth;
using OpsBoard.Api.Middleware;
using OpsBoard.Application.Abstractions;
using OpsBoard.Application.Auth;
using OpsBoard.Infrastructure.Auth;
using OpsBoard.Infrastructure.Persistence;
using OpsBoard.Infrastructure.Persistence.Seed;
using OpsBoard.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=opsboard;Username=postgres;Password=postgres";

builder.Services.AddDbContext<OpsBoardDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<ISystemClock, SystemClock>();
builder.Services.AddScoped<IUserLookup, EfUserLookup>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<AuditLogQueryService>();
builder.Services.AddScoped<MenuQueryService>();
builder.Services.AddScoped<TableQueryService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    context.Fail("Token does not contain a valid user id.");
                    return;
                }

                var userLookup = context.HttpContext.RequestServices.GetRequiredService<IUserLookup>();
                var activeUser = await userLookup.FindActiveByIdAsync(userId, context.HttpContext.RequestAborted);
                if (activeUser is null)
                {
                    context.Fail("User is inactive or no longer exists.");
                }
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.OrdersManage, policy =>
        policy.RequireRole("Admin", "Staff"));
    options.AddPolicy(AuthorizationPolicies.MenuManage, policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.UsersManage, policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.ReportsView, policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.AuditLogsView, policy =>
        policy.RequireRole("Admin"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await ApplyMigrationsAndSeedAsync(app);
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionMappingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

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

public partial class Program;
