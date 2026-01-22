using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using SecureTenant.Auth.Middleware;
using SecureTenant.Core.Entities;
using SecureTenant.Core.Interfaces;
using SecureTenant.Infrastructure.Data;
using SecureTenant.Infrastructure.Providers;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddHttpContextAccessor();

// Register TenantProvider and TenantService
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<ITenantService, SecureTenant.Infrastructure.Services.TenantService>();

// Configure Entity Framework and SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    
    // Configure OpenIddict to use EF Core
    options.UseOpenIddict();
});

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure OpenIddict
builder.Services.AddOpenIddict()
    // Register the OpenIddict core components
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })
    
    // Register the OpenIddict server components
    .AddServer(options =>
    {
        // Enable the authorization, token, userinfo, and introspection endpoints
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetUserInfoEndpointUris("/connect/userinfo")
               .SetIntrospectionEndpointUris("/connect/introspect");
        
        // Enable the authorization code flow with PKCE
        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();
        
        // Enable the client credentials flow
        options.AllowClientCredentialsFlow();
        
        // Enable the refresh token flow with sliding expiration
        options.AllowRefreshTokenFlow();
        
        // Configure refresh token settings
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(14))
               .SetAccessTokenLifetime(TimeSpan.FromMinutes(30));
        
        // Register claims
        options.RegisterClaims(Claims.Name, Claims.Email, Claims.Role, "tenant_id", "user_hierarchy", "membership_level");
        
        // Register scopes
        options.RegisterScopes(
            Scopes.OpenId,
            Scopes.Profile,
            Scopes.Email,
            Scopes.Roles,
            "api"
        );
        
        // Configure encryption and signing credentials
        // For development: use development signing and encryption keys
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();
        
        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options
        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableStatusCodePagesIntegration()
               .DisableTransportSecurityRequirement(); // Allow HTTP for development
    })
    
    // Register the OpenIddict validation components
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Add authentication
builder.Services.AddAuthentication();

// Add authorization
builder.Services.AddAuthorization();

// Add Razor Pages for UI
builder.Services.AddRazorPages();

// Add controllers
builder.Services.AddControllers();

// Add CORS for development (configure more restrictively for production)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // TODO: In production, replace with specific allowed origins
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors();

// Add tenant validation middleware
app.UseTenantValidation();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.MapGet("/", () => "SecureTenant Authorization Server is running!");

// Seed database and test data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.Run();

// Make Program class accessible for testing
public partial class Program { }
