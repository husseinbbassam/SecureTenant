using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SecureTenant.Core.Interfaces;
using SecureTenant.Infrastructure.Data;

namespace SecureTenant.Infrastructure.Services;

/// <summary>
/// Service for managing tenant context and validation with database access
/// </summary>
public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _dbContext;
    
    public TenantService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
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
        
        // Try to get tenant from subdomain and map it
        var host = httpContext.Request.Host.Host;
        if (!string.IsNullOrEmpty(host))
        {
            var parts = host.Split('.');
            if (parts.Length > 1)
            {
                var subdomain = parts[0].ToLowerInvariant();
                
                // Map subdomain to TenantId (e.g., tenantA -> TenantA, tenantB -> TenantB)
                if (subdomain.StartsWith("tenant"))
                {
                    // Capitalize first letter after "tenant"
                    var suffix = subdomain.Substring("tenant".Length);
                    if (!string.IsNullOrEmpty(suffix))
                    {
                        return "Tenant" + char.ToUpperInvariant(suffix[0]) + suffix.Substring(1);
                    }
                }
                
                // For other subdomains, return as-is (lowercase)
                return subdomain;
            }
        }
        
        return null;
    }
    
    public async Task<bool> ValidateTenantAsync(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
            return false;
        
        // Check if tenant exists and is active in the database
        // We need to query without tenant filter, so use IgnoreQueryFilters
        var tenant = await _dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive);
        
        return tenant != null;
    }
}
