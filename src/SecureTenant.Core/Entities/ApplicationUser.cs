using Microsoft.AspNetCore.Identity;

namespace SecureTenant.Core.Entities;

/// <summary>
/// Represents an application user with multi-tenancy support
/// </summary>
public class ApplicationUser : IdentityUser, ITenantEntity
{
    // Tenant discriminator for multi-tenancy
    public string TenantId { get; set; } = string.Empty;
    
    // Custom claims
    public string UserHierarchy { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation property
    public Tenant? Tenant { get; set; }
}
