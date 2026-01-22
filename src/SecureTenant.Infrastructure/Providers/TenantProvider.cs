using Microsoft.AspNetCore.Http;
using SecureTenant.Core.Interfaces;

namespace SecureTenant.Infrastructure.Providers;

/// <summary>
/// Resolves the current tenant from HTTP request context
/// Supports subdomain-based and header-based tenant resolution
/// </summary>
public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public string? GetCurrentTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;
        
        // Try to get tenant from custom header first
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            return tenantIdHeader.ToString();
        }
        
        // Try to get tenant from subdomain
        var host = httpContext.Request.Host.Host;
        if (!string.IsNullOrEmpty(host))
        {
            var parts = host.Split('.');
            if (parts.Length > 2)
            {
                // Extract subdomain (e.g., tenant1.example.com -> tenant1)
                return parts[0];
            }
        }
        
        return null;
    }
}
