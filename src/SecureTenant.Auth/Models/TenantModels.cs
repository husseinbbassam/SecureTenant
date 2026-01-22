namespace SecureTenant.Auth.Models;

/// <summary>
/// DTO for creating a new tenant
/// </summary>
public class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    
    public string Domain { get; set; } = string.Empty;
}

/// <summary>
/// DTO for tenant response
/// </summary>
public class TenantResponse
{
    public string Id { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public string Domain { get; set; } = string.Empty;
    
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
