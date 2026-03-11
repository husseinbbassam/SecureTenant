using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecureTenant.Core.Interfaces;
using SecureTenant.Core.Options;
using SecureTenant.Infrastructure.Data;
using SecureTenant.Infrastructure.Providers;
using SecureTenant.Infrastructure.Services;

namespace SecureTenant.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering SecureTenant infrastructure services.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all SecureTenant infrastructure services — tenant provider, tenant service,
    /// and <see cref="ApplicationDbContext"/> — using the supplied <paramref name="configureDb"/>
    /// action to configure the database provider.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureDb">
    /// An action to configure the <see cref="DbContextOptionsBuilder"/> for
    /// <see cref="ApplicationDbContext"/>. Use this to choose your database provider and any
    /// additional options (e.g. OpenIddict):
    /// <code>
    /// options => options.UseSqlite(connectionString).UseOpenIddict()
    /// </code>
    /// </param>
    /// <param name="configureOptions">
    /// An optional action to configure <see cref="SecureTenantOptions"/>. When omitted,
    /// defaults are used.
    /// </param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSecureTenantInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDb,
        Action<SecureTenantOptions>? configureOptions = null)
    {
        // Bind options — the delegate is additive so it stacks on top of any earlier
        // IConfiguration-based binding (e.g. from builder.Configuration.GetSection()).
        services.Configure<SecureTenantOptions>(opts => configureOptions?.Invoke(opts));

        services.AddHttpContextAccessor();

        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<ITenantService, TenantService>();

        services.AddDbContext<ApplicationDbContext>(configureDb);

        return services;
    }
}
