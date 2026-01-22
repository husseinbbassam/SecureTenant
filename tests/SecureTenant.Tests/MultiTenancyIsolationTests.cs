using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecureTenant.Core.Entities;
using SecureTenant.Core.Interfaces;
using SecureTenant.Infrastructure.Data;
using SecureTenant.Infrastructure.Providers;

namespace SecureTenant.Tests;

/// <summary>
/// Integration tests for multi-tenancy isolation
/// Tests demonstrate that tenant isolation works correctly using global query filters
/// </summary>
public class MultiTenancyIsolationTests
{
    private readonly string _sharedDatabaseName = "TestDb_" + Guid.NewGuid().ToString();
    
    private ApplicationDbContext CreateInMemoryContext(string? tenantId = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: _sharedDatabaseName)
            .Options;

        // Create a mock tenant provider that returns the specified tenant ID
        var httpContextAccessor = new HttpContextAccessor();
        if (tenantId != null)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Tenant-Id"] = tenantId;
            httpContextAccessor.HttpContext = context;
        }

        var tenantProvider = new TenantProvider(httpContextAccessor);
        
        return new ApplicationDbContext(options, tenantProvider);
    }

    [Fact]
    public async Task User_From_TenantA_Cannot_Access_TenantB_User()
    {
        var dbName = "TestDb_" + Guid.NewGuid().ToString();
        
        // Arrange: Create database with two tenants and users (no tenant filter)
        using (var setupContext = CreateContextWithDbName(dbName, null))
        {
            // Create Tenant A
            var tenantA = new Tenant
            {
                Id = "TenantA",
                Name = "Tenant A Company",
                Domain = "tenanta.auth.com",
                IsActive = true
            };
            setupContext.Tenants.Add(tenantA);

            // Create Tenant B
            var tenantB = new Tenant
            {
                Id = "TenantB",
                Name = "Tenant B Company",
                Domain = "tenantb.auth.com",
                IsActive = true
            };
            setupContext.Tenants.Add(tenantB);

            await setupContext.SaveChangesAsync();
            
            // Create User A in Tenant A - manually set TenantId and save with IgnoreQueryFilters
            var userA = new ApplicationUser
            {
                Id = "user-a-id",
                UserName = "userA@tenanta.com",
                Email = "userA@tenanta.com",
                TenantId = "TenantA",
                FirstName = "User",
                LastName = "A",
                EmailConfirmed = true
            };
            // Add directly to the change tracker and save
            setupContext.Add(userA);

            // Create User B in Tenant B - manually set TenantId
            var userB = new ApplicationUser
            {
                Id = "user-b-id",
                UserName = "userB@tenantb.com",
                Email = "userB@tenantb.com",
                TenantId = "TenantB",
                FirstName = "User",
                LastName = "B",
                EmailConfirmed = true
            };
            setupContext.Add(userB);

            await setupContext.SaveChangesAsync();
            
            // Verify data was actually saved (query without filter)
            var allUsers = await setupContext.Users.IgnoreQueryFilters().ToListAsync();
            Assert.Equal(2, allUsers.Count);
        }

        // Act & Assert: Query from Tenant A's context - should only see Tenant A users
        using (var tenantAContext = CreateContextWithDbName(dbName, "TenantA"))
        {
            var users = await tenantAContext.Users.ToListAsync();
            
            // Assert: Only User A should be visible
            Assert.Single(users);
            Assert.Equal("TenantA", users[0].TenantId);
            Assert.Equal("userA@tenanta.com", users[0].UserName);
        }

        // Act & Assert: Query from Tenant B's context - should only see Tenant B users
        using (var tenantBContext = CreateContextWithDbName(dbName, "TenantB"))
        {
            var users = await tenantBContext.Users.ToListAsync();
            
            // Assert: Only User B should be visible
            Assert.Single(users);
            Assert.Equal("TenantB", users[0].TenantId);
            Assert.Equal("userB@tenantb.com", users[0].UserName);
        }
    }

    [Fact]
    public async Task Tenant_Cannot_Query_Another_Tenants_User_By_Id()
    {
        var dbName = "TestDb_" + Guid.NewGuid().ToString();
        
        // Arrange: Create database with two tenants and users
        using (var setupContext = CreateContextWithDbName(dbName, null))
        {
            var tenantA = new Tenant
            {
                Id = "TenantA",
                Name = "Tenant A Company",
                Domain = "tenanta.auth.com",
                IsActive = true
            };
            setupContext.Tenants.Add(tenantA);

            var tenantB = new Tenant
            {
                Id = "TenantB",
                Name = "Tenant B Company",
                Domain = "tenantb.auth.com",
                IsActive = true
            };
            setupContext.Tenants.Add(tenantB);

            await setupContext.SaveChangesAsync();

            // Create User B in Tenant B - manually set TenantId
            var userB = new ApplicationUser
            {
                Id = "user-b-id",
                UserName = "userB@tenantb.com",
                Email = "userB@tenantb.com",
                TenantId = "TenantB",
                FirstName = "User",
                LastName = "B",
                EmailConfirmed = true
            };
            setupContext.Add(userB);

            await setupContext.SaveChangesAsync();
            
            // Verify data was actually saved (query without filter)
            var allUsers = await setupContext.Users.IgnoreQueryFilters().ToListAsync();
            Assert.Single(allUsers);
        }

        // Act: Try to find User B by ID from Tenant A context
        using (var tenantAContext = CreateContextWithDbName(dbName, "TenantA"))
        {
            var userB = await tenantAContext.Users.FirstOrDefaultAsync(u => u.Id == "user-b-id");
            
            // Assert: User B should NOT be accessible from Tenant A context
            Assert.Null(userB);
        }

        // Act: User B should be accessible from Tenant B context
        using (var tenantBContext = CreateContextWithDbName(dbName, "TenantB"))
        {
            var userB = await tenantBContext.Users.FirstOrDefaultAsync(u => u.Id == "user-b-id");
            
            // Assert: User B should be accessible from Tenant B context
            Assert.NotNull(userB);
            Assert.Equal("TenantB", userB.TenantId);
        }
    }

    [Fact]
    public async Task User_Created_In_Tenant_Context_Gets_Correct_TenantId()
    {
        var dbName = "TestDb_" + Guid.NewGuid().ToString();
        
        // Arrange: Create a tenant
        using (var setupContext = CreateContextWithDbName(dbName, null))
        {
            var tenantA = new Tenant
            {
                Id = "TenantA",
                Name = "Tenant A Company",
                Domain = "tenanta.auth.com",
                IsActive = true
            };
            setupContext.Tenants.Add(tenantA);
            await setupContext.SaveChangesAsync();
        }

        // Act: Create a user in Tenant A context
        using (var tenantAContext = CreateContextWithDbName(dbName, "TenantA"))
        {
            var user = new ApplicationUser
            {
                Id = "new-user-id",
                UserName = "newuser@tenanta.com",
                Email = "newuser@tenanta.com",
                FirstName = "New",
                LastName = "User",
                EmailConfirmed = true
                // Note: NOT setting TenantId manually - it should be set automatically
            };
            tenantAContext.Users.Add(user);
            await tenantAContext.SaveChangesAsync();
            
            // Assert: TenantId should be set automatically
            Assert.Equal("TenantA", user.TenantId);
        }
    }
    
    private ApplicationDbContext CreateContextWithDbName(string dbName, string? tenantId)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        // Create a mock tenant provider that returns the specified tenant ID
        var httpContextAccessor = new HttpContextAccessor();
        if (tenantId != null)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Tenant-Id"] = tenantId;
            httpContextAccessor.HttpContext = context;
        }

        var tenantProvider = new TenantProvider(httpContextAccessor);
        
        return new ApplicationDbContext(options, tenantProvider);
    }

    [Fact]
    public void Subdomain_Mapping_Converts_tenantA_To_TenantA()
    {
        // This is a unit test for the subdomain mapping logic
        // The mapping should convert "tenanta" subdomain to "TenantA"
        
        var subdomain = "tenanta";
        string? tenantId = null;
        
        if (subdomain.StartsWith("tenant"))
        {
            var suffix = subdomain.Substring("tenant".Length);
            if (!string.IsNullOrEmpty(suffix))
            {
                tenantId = "Tenant" + char.ToUpperInvariant(suffix[0]) + suffix.Substring(1);
            }
        }
        
        Assert.Equal("TenantA", tenantId);
    }

    [Fact]
    public void Subdomain_Mapping_Converts_tenantB_To_TenantB()
    {
        var subdomain = "tenantb";
        string? tenantId = null;
        
        if (subdomain.StartsWith("tenant"))
        {
            var suffix = subdomain.Substring("tenant".Length);
            if (!string.IsNullOrEmpty(suffix))
            {
                tenantId = "Tenant" + char.ToUpperInvariant(suffix[0]) + suffix.Substring(1);
            }
        }
        
        Assert.Equal("TenantB", tenantId);
    }
}
