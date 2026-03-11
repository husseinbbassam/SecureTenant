# SecureTenant

[![CI](https://github.com/husseinbbassam/SecureTenant/actions/workflows/ci.yml/badge.svg)](https://github.com/husseinbbassam/SecureTenant/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/9.0)

**SecureTenant** is a multi-tenant Identity Provider built with .NET 9, C# 13, and [OpenIddict](https://github.com/openiddict/openiddict-core). It provides secure authentication and authorization services with automatic tenant isolation using a single database with discriminator column approach.

## Architecture

The solution consists of four projects:

### 1. SecureTenant.Core
- Domain entities (`Tenant`, `ApplicationUser`) and interfaces
- `ITenantProvider` — abstracts how the current tenant is resolved
- `ITenantService` — validates tenant existence/status
- `SecureTenantOptions` — strongly-typed configuration class

### 2. SecureTenant.Infrastructure
- `ApplicationDbContext` with EF Core Global Query Filters for automatic per-tenant data isolation
- `TenantProvider` — resolves the tenant from an HTTP header or subdomain
- `TenantService` — validates the tenant against the database
- `AddSecureTenantInfrastructure()` — `IServiceCollection` extension for easy DI registration

### 3. SecureTenant.Auth
- OpenIddict Authorization Server
- Supports multiple OAuth 2.0 / OIDC flows:
  - **Authorization Code Flow with PKCE** (for end-users)
  - **Client Credentials Flow** (for machine-to-machine)
  - **Refresh Token Flow** with sliding expiration
- Custom JWT claims: `tenant_id`, `user_hierarchy`, `membership_level`
- `TenantValidationMiddleware` — validates the incoming tenant on every request
- OIDC Discovery at `/.well-known/openid-configuration`

### 4. SecureTenant.ProtectedApi
- Sample API demonstrating JWT Bearer authentication against the Auth server

## Features

### Multi-Tenancy
- **Single database, discriminator column** strategy
- Automatic tenant isolation via EF Core Global Query Filters
- Tenant resolved from:
  - `X-Tenant-Id` HTTP header (configurable)
  - Subdomain (e.g., `tenant1.example.com` → `tenant1`, configurable)

### Security
- OpenIddict 6.0 — OAuth 2.0 and OpenID Connect
- Authorization Code Flow with PKCE
- Client Credentials Flow
- Refresh tokens with configurable expiration
- Development signing/encryption certificates (swap for production certificates)

### Custom Claims
- `tenant_id` — Included in access and identity tokens
- `user_hierarchy` — Custom user hierarchy claim
- `membership_level` — Membership level claim
- Standard OIDC claims (`sub`, `name`, `email`, `role`)

## Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later

### Building and Running

```bash
# Clone the repository
git clone https://github.com/husseinbbassam/SecureTenant.git
cd SecureTenant

# Restore dependencies and build
dotnet restore
dotnet build

# Run the authorization server
cd src/SecureTenant.Auth
dotnet run
```

The server starts on `http://localhost:5200` (HTTP) and `https://localhost:7200` (HTTPS) by default.

### OIDC Discovery Document

```
GET http://localhost:5200/.well-known/openid-configuration
```

### Running the Tests

```bash
dotnet test
```

## Configuration

All SecureTenant settings live under a single `"SecureTenant"` key in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SecureTenant.db"
  },
  "SecureTenant": {
    "TenantResolution": {
      "HeaderName": "X-Tenant-Id",
      "EnableSubdomainResolution": true,
      "BypassPaths": [ "/.well-known", "/health", "/" ]
    },
    "Tokens": {
      "AccessTokenLifetime": "00:30:00",
      "RefreshTokenLifetime": "14.00:00:00"
    }
  }
}
```

### Switching the Database

The solution uses **SQLite** for development. To switch to SQL Server (or any other EF Core–supported provider):

1. Install the provider package:
   ```bash
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   ```
2. Update the connection string in `appsettings.json`
3. Change the `AddSecureTenantInfrastructure` call in `Program.cs`:
   ```csharp
   builder.Services.AddSecureTenantInfrastructure(options =>
       options.UseSqlServer(connectionString).UseOpenIddict());
   ```

## Integrating into Your Own Application

The `AddSecureTenantInfrastructure` extension method wires up all the services in one call:

```csharp
// Program.cs
builder.Services.Configure<SecureTenantOptions>(
    builder.Configuration.GetSection(SecureTenantOptions.SectionName));

builder.Services.AddSecureTenantInfrastructure(
    configureDb: options => options
        .UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseOpenIddict(),
    configureOptions: opts =>
    {
        opts.TenantResolution.HeaderName = "X-My-Tenant";
        opts.Tokens.AccessTokenLifetime = TimeSpan.FromHours(1);
    });
```

Then add the tenant-validation middleware:

```csharp
app.UseRouting();
app.UseTenantValidation(); // after routing, before authentication
app.UseAuthentication();
app.UseAuthorization();
```

### Custom Tenant Resolution

Implement `ITenantProvider` to resolve the tenant from any source (e.g., a JWT claim):

```csharp
public class ClaimsTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsTenantProvider(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public string? GetCurrentTenantId()
        => _httpContextAccessor.HttpContext?.User.FindFirstValue("tenant_id");
}
```

Register your implementation before calling `AddSecureTenantInfrastructure` (or replace the registration afterwards):

```csharp
builder.Services.AddScoped<ITenantProvider, ClaimsTenantProvider>();
```

### Adding a New Multi-Tenant Entity

1. Implement `ITenantEntity`:
   ```csharp
   public class Product : ITenantEntity
   {
       public int Id { get; set; }
       public string Name { get; set; } = string.Empty;
       public string TenantId { get; set; } = string.Empty;
   }
   ```
2. Add a `DbSet<Product>` to `ApplicationDbContext`
3. The Global Query Filter is applied automatically by `ApplyGlobalFilters`

## Project Structure

```
SecureTenant/
├── src/
│   ├── SecureTenant.Core/           # Domain models, interfaces, options
│   │   ├── Entities/
│   │   │   ├── Tenant.cs
│   │   │   ├── ApplicationUser.cs
│   │   │   └── ITenantEntity.cs
│   │   ├── Interfaces/
│   │   │   ├── ITenantProvider.cs
│   │   │   └── ITenantService.cs
│   │   └── Options/
│   │       └── SecureTenantOptions.cs
│   ├── SecureTenant.Infrastructure/ # Data access, providers, DI extensions
│   │   ├── Data/
│   │   │   └── ApplicationDbContext.cs
│   │   ├── Extensions/
│   │   │   └── InfrastructureServiceExtensions.cs
│   │   ├── Providers/
│   │   │   └── TenantProvider.cs
│   │   └── Services/
│   │       └── TenantService.cs
│   ├── SecureTenant.Auth/           # OpenIddict Authorization Server host
│   │   ├── Controllers/
│   │   │   ├── AuthorizationController.cs
│   │   │   └── AdminController.cs
│   │   ├── Middleware/
│   │   │   └── TenantValidationMiddleware.cs
│   │   └── Program.cs
│   └── SecureTenant.ProtectedApi/   # Sample protected API
└── tests/
    └── SecureTenant.Tests/          # Integration tests
```

## Key Technologies

| Technology | Version | Purpose |
|-----------|---------|---------|
| **.NET** | 9.0 | Runtime |
| **C#** | 13 | Language |
| **OpenIddict** | 6.0 | OAuth 2.0 / OIDC server |
| **Entity Framework Core** | 9.0 | ORM + multi-tenant data isolation |
| **ASP.NET Core Identity** | 9.0 | User management |
| **SQLite** | — | Development database |

## OAuth 2.0 Endpoints

| Endpoint | Path |
|----------|------|
| Discovery document | `GET /.well-known/openid-configuration` |
| Authorization | `GET /connect/authorize` |
| Token | `POST /connect/token` |
| UserInfo | `GET|POST /connect/userinfo` |
| Introspection | `POST /connect/introspect` |
| JWKS | `GET /.well-known/jwks` |
| Admin — create tenant | `POST /admin/tenants` |
| Admin — get tenant | `GET /admin/tenants/{id}` |
| Admin — list tenants | `GET /admin/tenants` |

## Development Notes

### HTTP in Development

The server is configured to accept plain HTTP requests via:
```csharp
options.UseAspNetCore()
       .DisableTransportSecurityRequirement();
```

**Remove `DisableTransportSecurityRequirement()` in production and use HTTPS only.**

### Multi-Tenant Data Isolation

All queries on entities implementing `ITenantEntity` are automatically filtered by the current tenant:

```csharp
entity.HasQueryFilter(e => e.TenantId == _tenantProvider.GetCurrentTenantId());
```

`SaveChanges` / `SaveChangesAsync` also automatically set `TenantId` on any newly `Added` entities.

### CORS in Development

The default policy allows any origin for development convenience. **Replace with specific origins for production.**

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## Code of Conduct

Please read our [Code of Conduct](CODE_OF_CONDUCT.md) before contributing.

## License

This project is licensed under the [MIT License](LICENSE).
