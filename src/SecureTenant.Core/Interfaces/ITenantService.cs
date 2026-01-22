namespace SecureTenant.Core.Interfaces;

/// <summary>
/// Service for managing tenant context and validation
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Gets the current tenant ID from the request context
    /// </summary>
    string? GetCurrentTenantId();
    
    /// <summary>
    /// Validates if a tenant exists and is active
    /// </summary>
    Task<bool> ValidateTenantAsync(string tenantId);
}
