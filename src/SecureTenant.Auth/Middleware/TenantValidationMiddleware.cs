using Microsoft.Extensions.Options;
using SecureTenant.Core.Interfaces;
using SecureTenant.Core.Options;

namespace SecureTenant.Auth.Middleware;

/// <summary>
/// Middleware that validates the tenant from the incoming request.
/// Bypass paths and header name are configured via <see cref="SecureTenantOptions"/>.
/// </summary>
public class TenantValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IList<string> _bypassPaths;

    public TenantValidationMiddleware(RequestDelegate next, IOptions<SecureTenantOptions> options)
    {
        _next = next;
        _bypassPaths = options.Value.TenantResolution.BypassPaths;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var tenantId = tenantService.GetCurrentTenantId();

        // Skip validation for configured bypass paths
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        foreach (var bypassPath in _bypassPaths)
        {
            if (path.StartsWith(bypassPath, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
        }

        // If a tenant ID is provided, validate it
        if (!string.IsNullOrEmpty(tenantId))
        {
            var isValid = await tenantService.ValidateTenantAsync(tenantId);
            if (!isValid)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "unauthorized_tenant",
                    error_description = "The tenant is not recognized or inactive."
                });
                return;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering <see cref="TenantValidationMiddleware"/>.
/// </summary>
public static class TenantValidationMiddlewareExtensions
{
    /// <summary>
    /// Adds the SecureTenant tenant-validation middleware to the request pipeline.
    /// Call this after <c>UseRouting()</c> and before <c>UseAuthentication()</c>.
    /// </summary>
    public static IApplicationBuilder UseTenantValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantValidationMiddleware>();
    }
}
