using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureTenant.Auth.Models;
using SecureTenant.Core.Entities;
using SecureTenant.Infrastructure.Data;

namespace SecureTenant.Auth.Controllers;

/// <summary>
/// Admin controller for tenant management
/// </summary>
[ApiController]
[Route("admin")]
[Authorize(Roles = "GlobalAdmin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext dbContext, ILogger<AdminController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new tenant (onboarding)
    /// </summary>
    [HttpPost("tenants")]
    public async Task<ActionResult<TenantResponse>> CreateTenant([FromBody] CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Tenant name is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Domain))
        {
            return BadRequest(new { error = "Tenant domain is required" });
        }

        // Check if tenant with same domain already exists
        var existingTenant = await _dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Domain == request.Domain);

        if (existingTenant != null)
        {
            return Conflict(new { error = "A tenant with this domain already exists" });
        }

        var tenant = new Tenant
        {
            Name = request.Name,
            Domain = request.Domain,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Tenant created: {TenantId} - {TenantName}", tenant.Id, tenant.Name);

        var response = new TenantResponse
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Domain = tenant.Domain,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt
        };

        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, response);
    }

    /// <summary>
    /// Get a tenant by ID
    /// </summary>
    [HttpGet("tenants/{id}")]
    public async Task<ActionResult<TenantResponse>> GetTenant(string id)
    {
        var tenant = await _dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null)
        {
            return NotFound();
        }

        var response = new TenantResponse
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Domain = tenant.Domain,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// List all tenants
    /// </summary>
    [HttpGet("tenants")]
    public async Task<ActionResult<IEnumerable<TenantResponse>>> ListTenants()
    {
        var tenants = await _dbContext.Tenants
            .IgnoreQueryFilters()
            .ToListAsync();

        var response = tenants.Select(t => new TenantResponse
        {
            Id = t.Id,
            Name = t.Name,
            Domain = t.Domain,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt
        });

        return Ok(response);
    }
}
