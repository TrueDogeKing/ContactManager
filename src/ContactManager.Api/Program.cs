using System.Text;
using ContactManager.Application;
using ContactManager.Infrastructure;
using ContactManager.Infrastructure.Auth;
using ContactManager.Infrastructure.Persistence;
using ContactManager.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Warstwa aplikacji (serwisy, walidatory) i infrastruktury (EF Core / PostgreSQL).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Uwierzytelnianie JWT.
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Brak sekcji konfiguracji 'Jwt'.");
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

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
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Automatyczne zastosowanie migracji (np. przy starcie kontenera) – sterowane configiem.
if (app.Configuration.GetValue<bool>("Database:MigrateAutomatically"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Seed danych początkowych (domyślny administrator) – sterowane configiem.
if (app.Configuration.GetValue<bool>("Database:SeedAutomatically"))
{
    await DataSeeder.SeedAdminUserAsync(app.Services);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
