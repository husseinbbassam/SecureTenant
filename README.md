# SecureTenant

**SecureTenant** is a Multi-Tenant Identity Provider built with .NET 9, C# 13, and OpenIddict. It provides secure authentication and authorization services with tenant isolation using a single database with discriminator column approach.

## Architecture

The solution consists of four main projects:

### 1. SecureTenant.Core
- Contains domain entities and interfaces
- Defines `Tenant` and `ApplicationUser` entities
- Implements `ITenantProvider` interface for tenant resolution
- Provides base interfaces for multi-tenant entities

### 2. SecureTenant.Infrastructure  
- Implements EF Core data access layer
- Configures `ApplicationDbContext` with multi-tenant support
- Implements `TenantProvider` for resolving tenant from:
  - HTTP header (`X-Tenant-Id`)
  - Subdomain extraction
- Uses **EF Core Global Query Filters** for automatic tenant isolation

### 3. SecureTenant.Auth
- OpenIddict Authorization Server
- Supports multiple OAuth 2.0 flows:
  - **Authorization Code Flow with PKCE** (for users)
  - **Client Credentials Flow** (for machine-to-machine)
  - **Refresh Token Flow** with sliding expiration
- Custom JWT claims:
  - `tenant_id` - Identifies the tenant
  - `user_hierarchy` - User hierarchy information
- OIDC Discovery endpoint at `/.well-known/openid-configuration`

### 4. SecureTenant.ProtectedApi
- Sample protected API demonstrating JWT Bearer authentication
- Shows how to consume tokens from SecureTenant

## Features

### Multi-Tenancy
- **Single Database, Discriminator Column** strategy
- Automatic tenant isolation via EF Core Global Query Filters
- Tenant resolution from:
  - `X-Tenant-Id` HTTP header
  - Subdomain (e.g., `tenant1.example.com`)

### Security
- OpenIddict 6.0 implementation
- Development signing and encryption certificates
- Refresh tokens with configurable expiration
- Authorization Code Flow with PKCE
- Client Credentials Flow

### Custom Claims
- `tenant_id` - Included in access and identity tokens
- `user_hierarchy` - Custom user hierarchy claim
- Standard OIDC claims (name, email, roles)

## Getting Started

### Prerequisites
- .NET 9 SDK or later
- SQLite (for development)

### Building the Solution

```bash
dotnet build
```

### Running the Authorization Server

```bash
cd src/SecureTenant.Auth
dotnet run
```

The server will start on `http://localhost:5400` (or as configured in `launchSettings.json`).

### OIDC Discovery Document

Access the OpenID Connect discovery document at:

```
http://localhost:5400/.well-known/openid-configuration
```

Example response:
```json
{
    "issuer": "http://localhost:5400/",
    "authorization_endpoint": "http://localhost:5400/connect/authorize",
    "token_endpoint": "http://localhost:5400/connect/token",
    "userinfo_endpoint": "http://localhost:5400/connect/userinfo",
    "grant_types_supported": [
        "authorization_code",
        "client_credentials",
        "refresh_token"
    ],
    "claims_supported": [
        "sub", "name", "email", "role",
        "tenant_id", "user_hierarchy"
    ],
    "scopes_supported": [
        "openid", "profile", "email", "roles", "api"
    ]
}
```

## Configuration

### Database

The solution uses SQLite for development. Connection string is configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SecureTenant.db"
  }
}
```

For production, switch to SQL Server or another database provider by:
1. Installing the appropriate EF Core provider package
2. Updating the connection string
3. Modifying `Program.cs` to use the new provider

### Token Lifetimes

Configured in `Program.cs`:
- **Access Token**: 30 minutes
- **Refresh Token**: 14 days

## Project Structure

```
SecureTenant/
├── src/
│   ├── SecureTenant.Core/           # Domain models and interfaces
│   │   ├── Entities/
│   │   │   ├── Tenant.cs
│   │   │   ├── ApplicationUser.cs
│   │   │   └── ITenantEntity.cs
│   │   └── Interfaces/
│   │       └── ITenantProvider.cs
│   ├── SecureTenant.Infrastructure/ # Data access and implementations
│   │   ├── Data/
│   │   │   └── ApplicationDbContext.cs
│   │   └── Providers/
│   │       └── TenantProvider.cs
│   ├── SecureTenant.Auth/          # Authorization Server
│   │   ├── Controllers/
│   │   │   └── AuthorizationController.cs
│   │   └── Program.cs
│   └── SecureTenant.ProtectedApi/  # Sample protected API
└── SecureTenant.sln
```

## Key Technologies

- **.NET 9** - Latest .NET framework
- **C# 13** - Latest C# language features
- **OpenIddict 6.0** - OAuth 2.0 and OpenID Connect server
- **Entity Framework Core 9** - ORM with multi-tenant support
- **ASP.NET Core Identity** - User management
- **SQLite** - Development database

## Development Notes

### HTTP vs HTTPS

For development, the server is configured to accept HTTP requests using:
```csharp
options.UseAspNetCore()
       .DisableTransportSecurityRequirement();
```

**Important:** For production, remove this configuration and use HTTPS only.

### Multi-Tenant Data Isolation

All queries to `ApplicationUser` are automatically filtered by the current tenant:

```csharp
entity.HasQueryFilter(e => e.TenantId == _tenantProvider.GetCurrentTenantId());
```

When adding new entities, ensure they:
1. Implement `ITenantEntity` interface
2. Have a `TenantId` property
3. Are configured with a global query filter in `ApplicationDbContext`

## Endpoints

### Authorization Server

- `GET /.well-known/openid-configuration` - Discovery document
- `POST /connect/token` - Token endpoint
- `GET /connect/authorize` - Authorization endpoint
- `GET|POST /connect/userinfo` - UserInfo endpoint
- `POST /connect/introspect` - Token introspection

## Next Steps

1. Implement Protected API with JWT Bearer authentication
2. Add user registration and management endpoints
3. Create admin portal for tenant management
4. Add comprehensive integration tests
5. Implement BFF (Backend-for-Frontend) pattern
6. Add proper HTTPS certificates for production
7. Implement database migrations
8. Add logging and monitoring

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]