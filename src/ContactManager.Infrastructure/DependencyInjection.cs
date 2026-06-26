using ContactManager.Application.Interfaces;
using ContactManager.Domain.Repositories;
using ContactManager.Infrastructure.Auth;
using ContactManager.Infrastructure.Persistence;
using ContactManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContactManager.Infrastructure;

/// Registration of infrastructure services in the DI container.
public static class DependencyInjection
{
    /// Registers <see cref="Persistence.AppDbContext"/> with a PostgreSQL (Npgsql) provider
    /// and other infrastructure services.
    /// <param name="services">Kolekcja usług.</param>
    /// <param name="configuration">Konfiguracja aplikacji (źródło connection stringa).</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        );

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        return services;
    }
}
