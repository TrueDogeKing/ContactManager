using ContactManager.Application.Interfaces;
using ContactManager.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ContactManager.Application;

/// <summary>
/// Rejestracja usług warstwy aplikacji (logika biznesowa, walidatory).
/// </summary>
public static class DependencyInjection
{
    /// <summary>Rejestruje serwisy i walidatory warstwy aplikacji.</summary>
    /// <param name="services">Kolekcja usług.</param>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
