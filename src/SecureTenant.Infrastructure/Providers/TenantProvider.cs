using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SecureTenant.Core.Interfaces;
using SecureTenant.Core.Options;

namespace SecureTenant.Infrastructure.Providers;

/// <summary>
/// Resolves the current tenant from HTTP request context.
/// Supports header-based and subdomain-based tenant resolution,
/// both of which are configurable via <see cref="SecureTenantOptions"/>.
/// </summary>
public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TenantResolutionOptions _resolutionOptions;

    public TenantProvider(
        IHttpContextAccessor httpContextAccessor,
        IOptions<SecureTenantOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _resolutionOptions = options.Value.TenantResolution;
    }

    public string? GetCurrentTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        // Try to get tenant from the configured header first
        if (httpContext.Request.Headers.TryGetValue(_resolutionOptions.HeaderName, out var tenantIdHeader))
        {
            return tenantIdHeader.ToString();
        }

        // Optionally resolve tenant from subdomain (e.g., tenant1.example.com -> tenant1)
        if (_resolutionOptions.EnableSubdomainResolution)
        {
            var host = httpContext.Request.Host.Host;
            if (!string.IsNullOrEmpty(host))
            {
                var parts = host.Split('.');
                if (parts.Length > 2)
                {
                    return parts[0];
                }
            }
        }

        return null;
    }
}
