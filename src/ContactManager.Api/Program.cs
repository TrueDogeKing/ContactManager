using ContactManager.Infrastructure;
using ContactManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Warstwa infrastruktury (EF Core / PostgreSQL).
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Automatyczne zastosowanie migracji (np. przy starcie kontenera) – sterowane configiem.
if (app.Configuration.GetValue<bool>("Database:MigrateAutomatically"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
