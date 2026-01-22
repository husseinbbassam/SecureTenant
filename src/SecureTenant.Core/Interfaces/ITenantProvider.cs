namespace SecureTenant.Core.Interfaces;

/// <summary>
/// Interface for resolving the current tenant from the HTTP request
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the current tenant ID from the request context
    /// </summary>
    string? GetCurrentTenantId();
}
