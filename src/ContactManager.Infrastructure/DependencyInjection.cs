using ContactManager.Application.Interfaces;
using ContactManager.Domain.Repositories;
using ContactManager.Infrastructure.Auth;
using ContactManager.Infrastructure.Persistence;
using ContactManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContactManager.Infrastructure;

/// <summary>
/// Rejestracja usług warstwy infrastruktury w kontenerze DI.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Rejestruje <see cref="Persistence.AppDbContext"/> z dostawcą PostgreSQL (Npgsql)
    /// oraz pozostałe usługi infrastruktury.
    /// </summary>
    /// <param name="services">Kolekcja usług.</param>
    /// <param name="configuration">Konfiguracja aplikacji (źródło connection stringa).</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
