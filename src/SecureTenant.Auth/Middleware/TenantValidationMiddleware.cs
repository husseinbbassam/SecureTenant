using SecureTenant.Core.Interfaces;

namespace SecureTenant.Auth.Middleware;

/// <summary>
/// Middleware that validates tenant from the incoming request URL
/// </summary>
public class TenantValidationMiddleware
{
    private readonly RequestDelegate _next;
    
    public TenantValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        
        // Skip validation for certain endpoints (e.g., health checks, well-known endpoints)
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (path.StartsWith("/.well-known") || 
            path == "/" || 
            path.StartsWith("/health"))
        {
            await _next(context);
            return;
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
/// Extension methods for registering TenantValidationMiddleware
/// </summary>
public static class TenantValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantValidationMiddleware>();
    }
}
