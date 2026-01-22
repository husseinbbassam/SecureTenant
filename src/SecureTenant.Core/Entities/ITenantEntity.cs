namespace SecureTenant.Core.Entities;

/// <summary>
/// Base entity for multi-tenant entities that need tenant isolation
/// </summary>
public interface ITenantEntity
{
    string TenantId { get; set; }
}
