using ContactManager.Application.Interfaces;
using ContactManager.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ContactManager.Application;

public static class DependencyInjection
{
    /// Register services and validators application layer.
    /// <param name="services">Service collection.</param>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
