using Infrastructure.UseCases.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

/// <summary>
/// Extension method that registers all Infrastructure-layer services with the DI container.
///
/// WHY an extension method:
///   Keeps Program.cs clean. All Infrastructure wiring lives here. When you add
///   new use cases, services, or adapters, you add them here — Program.cs never
///   changes for infrastructure concerns.
///
/// Registration lifetime — Scoped:
///   Use cases are registered as Scoped (one instance per HTTP request) because
///   they depend on AppDbContext (also scoped) and UserManager/SignInManager (scoped).
///   Registering them as Singleton would cause a "captured dependency" bug where
///   a long-lived object holds a short-lived DbContext.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // ── Auth use cases ────────────────────────────────────────────────────
        services.AddScoped<LoginUseCase>();
        services.AddScoped<MeUseCase>();

        // ── Add future use cases below, grouped by controller/feature ─────────
        // e.g.:
        // services.AddScoped<RegisterUseCase>();
        // services.AddScoped<GetProductsUseCase>();

        return services;
    }
}
