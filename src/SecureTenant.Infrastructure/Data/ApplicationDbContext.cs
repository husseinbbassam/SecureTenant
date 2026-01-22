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
            entity.Property(e => e.MembershipLevel).HasMaxLength(100);
            
            entity.HasIndex(e => e.TenantId);
        });
        
        // Apply Global Query Filter to all entities implementing ITenantEntity
        ApplyGlobalFilters(builder);
    }
    
    private void ApplyGlobalFilters(ModelBuilder builder)
    {
        // Get all entity types that implement ITenantEntity
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Create the filter expression: e => e.TenantId == _tenantProvider.GetCurrentTenantId()
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                
                // Create a call to _tenantProvider.GetCurrentTenantId() - this will be evaluated at query time
                var tenantProviderConst = System.Linq.Expressions.Expression.Constant(_tenantProvider);
                var getCurrentTenantIdMethod = typeof(ITenantProvider).GetMethod(nameof(ITenantProvider.GetCurrentTenantId));
                var tenantIdCall = System.Linq.Expressions.Expression.Call(tenantProviderConst, getCurrentTenantIdMethod!);
                
                var comparison = System.Linq.Expressions.Expression.Equal(property, tenantIdCall);
                var lambda = System.Linq.Expressions.Expression.Lambda(comparison, parameter);
                
                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
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
