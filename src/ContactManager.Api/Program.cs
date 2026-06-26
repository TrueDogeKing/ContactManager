using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using ContactManager.Api.Errors;
using ContactManager.Api.OpenApi;
using ContactManager.Api.RateLimiting;
using ContactManager.Application;
using ContactManager.Infrastructure;
using ContactManager.Infrastructure.Auth;
using ContactManager.Infrastructure.Persistence;
using ContactManager.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>()
);
builder.Services.AddHealthChecks();

// CORS for the frontend. Credentials are allowed (refresh token cookie), so origins must be explicit.
var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
builder.Services.AddCors(options =>
    options.AddPolicy(
        FrontendCorsPolicy,
        policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()
    )
);

// Global exception handling with ProblemDetails responses.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Uwierzytelnianie JWT.
var jwtSettings =
    builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Brak sekcji konfiguracji 'Jwt'.");
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<RefreshTokenSettings>(
    builder.Configuration.GetSection(RefreshTokenSettings.SectionName)
);

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero,
        };
    });
builder.Services.AddAuthorization();

// Rate limiting: brute-force protection for the authentication endpoints, partitioned by client IP.
// Behind a reverse proxy/ingress, configure forwarded headers so RemoteIpAddress is the real client.
var authPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:Auth:PermitLimit") ?? 5;
var authWindowSeconds =
    builder.Configuration.GetValue<int?>("RateLimiting:Auth:WindowSeconds") ?? 30;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // On rejection, advertise when the client may retry (seconds) via the Retry-After header,
    // so the frontend can show an accurate countdown.
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = (
                (int)Math.Ceiling(retryAfter.TotalSeconds)
            ).ToString(CultureInfo.InvariantCulture);
        }
        return ValueTask.CompletedTask;
    };

    options.AddPolicy(
        RateLimitPolicies.Auth,
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authPermitLimit,
                    Window = TimeSpan.FromSeconds(authWindowSeconds),
                    QueueLimit = 0,
                }
            )
    );
});

var app = builder.Build();

// Automated migration on container start.
if (app.Configuration.GetValue<bool>("Database:MigrateAutomatically"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Seed initial data (default admin + sample contacts)
if (app.Configuration.GetValue<bool>("Database:SeedAutomatically"))
{
    await DataSeeder.SeedAdminUserAsync(app.Services);
    await DataSeeder.SeedContactsAsync(app.Services);
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors(FrontendCorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// Exposed so the integration test project can target it with WebApplicationFactory<Program>.
public partial class Program;
