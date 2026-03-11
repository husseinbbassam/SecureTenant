namespace SecureTenant.Core.Options;

/// <summary>
/// Top-level configuration options for SecureTenant, typically bound from
/// <c>appsettings.json</c> under the <c>"SecureTenant"</c> key.
/// </summary>
public class SecureTenantOptions
{
    /// <summary>
    /// The configuration section key used when binding from <c>IConfiguration</c>.
    /// </summary>
    public const string SectionName = "SecureTenant";

    /// <summary>
    /// Controls how the current tenant is resolved from an incoming HTTP request.
    /// </summary>
    public TenantResolutionOptions TenantResolution { get; set; } = new();

    /// <summary>
    /// Configures token lifetime settings for the authorization server.
    /// </summary>
    public TokenOptions Tokens { get; set; } = new();
}

/// <summary>
/// Options that govern how the current tenant is resolved from an HTTP request.
/// </summary>
public class TenantResolutionOptions
{
    /// <summary>
    /// The HTTP request header name used to pass the tenant identifier.
    /// Defaults to <c>"X-Tenant-Id"</c>.
    /// </summary>
    public string HeaderName { get; set; } = "X-Tenant-Id";

    /// <summary>
    /// When <see langword="true"/>, the tenant is also resolved from the
    /// request hostname subdomain (e.g. <c>tenant1.example.com</c> → <c>tenant1</c>).
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableSubdomainResolution { get; set; } = true;

    /// <summary>
    /// Paths that should bypass tenant validation, expressed as path prefixes.
    /// Defaults to <c>["/.well-known", "/health", "/"]</c>.
    /// </summary>
    public IList<string> BypassPaths { get; set; } = ["/.well-known", "/health", "/"];
}

/// <summary>
/// Options that control token lifetimes issued by the authorization server.
/// </summary>
public class TokenOptions
{
    /// <summary>
    /// The lifetime of access tokens. Defaults to 30 minutes.
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// The lifetime of refresh tokens. Defaults to 14 days.
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(14);
}
