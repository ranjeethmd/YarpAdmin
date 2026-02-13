using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace YarpAdmin.Middleware;

/// <summary>
/// Middleware for handling YARP Admin authentication and authorization.
/// </summary>
public class YarpAdminAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly YarpAdminOptions _options;
    private readonly ILogger<YarpAdminAuthMiddleware> _logger;

    public YarpAdminAuthMiddleware(
        RequestDelegate next,
        YarpAdminOptions options,
        ILogger<YarpAdminAuthMiddleware> logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this is a YARP Admin request
        if (context.Request.Path.StartsWithSegments("/api/yarp-admin") ||
            context.Request.Path.StartsWithSegments("/yarp-admin"))
        {
            // Check authentication if required
            if (_options.RequireAuthentication)
            {
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    _logger.LogWarning("Unauthenticated access attempt to YARP Admin from {IP}", 
                        context.Connection.RemoteIpAddress);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { message = "Authentication required" });
                    return;
                }

                // Check authorization policy if specified
                if (!string.IsNullOrEmpty(_options.AuthenticationPolicy))
                {
                    var authService = context.RequestServices
                        .GetService(typeof(Microsoft.AspNetCore.Authorization.IAuthorizationService)) 
                        as Microsoft.AspNetCore.Authorization.IAuthorizationService;

                    if (authService != null)
                    {
                        var result = await authService.AuthorizeAsync(
                            context.User, 
                            null, 
                            _options.AuthenticationPolicy);

                        if (!result.Succeeded)
                        {
                            _logger.LogWarning("Unauthorized access attempt to YARP Admin by {User}", 
                                context.User.Identity?.Name);
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsJsonAsync(new { message = "Access denied" });
                            return;
                        }
                    }
                }
            }

            // Log access
            _logger.LogInformation("YARP Admin accessed by {User} from {IP}: {Method} {Path}",
                context.User.Identity?.Name ?? "anonymous",
                context.Connection.RemoteIpAddress,
                context.Request.Method,
                context.Request.Path);
        }

        await _next(context);
    }
}

/// <summary>
/// Middleware for logging YARP Admin API requests.
/// </summary>
public class YarpAdminLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<YarpAdminLoggingMiddleware> _logger;

    public YarpAdminLoggingMiddleware(RequestDelegate next, ILogger<YarpAdminLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/yarp-admin"))
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "YARP Admin API: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
        else
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension methods for adding YARP Admin middleware.
/// </summary>
public static class YarpAdminMiddlewareExtensions
{
    /// <summary>
    /// Adds the YARP Admin authentication middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseYarpAdminAuth(this IApplicationBuilder app)
    {
        return app.UseMiddleware<YarpAdminAuthMiddleware>();
    }

    /// <summary>
    /// Adds the YARP Admin logging middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseYarpAdminLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<YarpAdminLoggingMiddleware>();
    }
}
