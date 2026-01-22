using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecureTenant.Core.Entities;
using SecureTenant.Core.Interfaces;

namespace SecureTenant.Infrastructure.Data;

/// <summary>
/// Main database context with multi-tenant support and OpenIddict integration
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ITenantProvider _tenantProvider;
    
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }
    
    public DbSet<Tenant> Tenants { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure Tenant entity
        builder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Domain).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Domain).IsUnique();
            
            // Relationship with Users
            entity.HasMany(e => e.Users)
                  .WithOne(u => u.Tenant)
                  .HasForeignKey(u => u.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure ApplicationUser entity
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.UserHierarchy).HasMaxLength(500);
            
            entity.HasIndex(e => e.TenantId);
            
            // Global Query Filter for multi-tenancy
            entity.HasQueryFilter(e => e.TenantId == _tenantProvider.GetCurrentTenantId());
        });
    }
    
    public override int SaveChanges()
    {
        ApplyTenantId();
        return base.SaveChanges();
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantId();
        return base.SaveChangesAsync(cancellationToken);
    }
    
    private void ApplyTenantId()
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (string.IsNullOrEmpty(tenantId))
            return;
        
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is ITenantEntity && e.State == EntityState.Added);
        
        foreach (var entry in entries)
        {
            if (entry.Entity is ITenantEntity tenantEntity)
            {
                tenantEntity.TenantId = tenantId;
            }
        }
    }
}
