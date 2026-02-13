using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using Yarp.ReverseProxy.Configuration;

namespace YarpAdmin;

/// <summary>
/// Extension methods for adding YARP Admin UI to your application.
/// </summary>
public static class YarpAdminExtensions
{
    /// <summary>
    /// Adds YARP Admin services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddYarpAdmin(
        this IServiceCollection services,
        Action<YarpAdminOptions>? configure = null)
    {
        var options = new YarpAdminOptions();
        configure?.Invoke(options);
        
        services.AddSingleton(options);
        services.AddSingleton<IYarpConfigurationStore, InMemoryYarpConfigurationStore>();
        services.AddSingleton<YarpAdminService>();
        services.AddSingleton<IYarpAdminService>(sp => sp.GetRequiredService<YarpAdminService>());
        services.AddSingleton<IProxyConfigProvider>(sp => sp.GetRequiredService<YarpAdminService>());

        // Add controllers from this assembly
        services.AddControllers()
            .AddApplicationPart(typeof(YarpAdminExtensions).Assembly);

        return services;
    }

    /// <summary>
    /// Adds YARP Admin services with a custom configuration store.
    /// </summary>
    /// <typeparam name="TStore">The type of configuration store to use.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddYarpAdmin<TStore>(
        this IServiceCollection services,
        Action<YarpAdminOptions>? configure = null)
        where TStore : class, IYarpConfigurationStore
    {
        var options = new YarpAdminOptions();
        configure?.Invoke(options);
        
        services.AddSingleton(options);
        services.AddSingleton<IYarpConfigurationStore, TStore>();
        services.AddSingleton<YarpAdminService>();
        services.AddSingleton<IYarpAdminService>(sp => sp.GetRequiredService<YarpAdminService>());
        services.AddSingleton<IProxyConfigProvider>(sp => sp.GetRequiredService<YarpAdminService>());

        services.AddControllers()
            .AddApplicationPart(typeof(YarpAdminExtensions).Assembly);

        return services;
    }

    /// <summary>
    /// Maps YARP Admin endpoints and UI.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pathPrefix">The path prefix for the admin UI (default: "/yarp-admin").</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapYarpAdmin(
        this IEndpointRouteBuilder endpoints,
        string pathPrefix = "/yarp-admin")
    {
        var options = endpoints.ServiceProvider.GetService<YarpAdminOptions>() 
            ?? new YarpAdminOptions();
        
        // Map API controllers
        endpoints.MapControllers();
        
        // Map the admin UI
        endpoints.MapYarpAdminUI(pathPrefix, options);
        
        return endpoints;
    }

    private static void MapYarpAdminUI(
        this IEndpointRouteBuilder endpoints, 
        string pathPrefix,
        YarpAdminOptions options)
    {
        // Serve the embedded admin UI
        endpoints.MapGet(pathPrefix, async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(GetAdminHtml(pathPrefix, options));
        });

        endpoints.MapGet($"{pathPrefix}/app.jsx", async context =>
        {
            context.Response.ContentType = "application/javascript";
            var assembly = typeof(YarpAdminExtensions).Assembly;
            var resourceName = "YarpAdmin.app.jsx";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                await context.Response.WriteAsync(await reader.ReadToEndAsync());
            }
        });
    }

    private static string GetAdminHtml(string pathPrefix, YarpAdminOptions options)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{options.Title}</title>
    <script src=""https://unpkg.com/react@18/umd/react.production.min.js"" crossorigin></script>
    <script src=""https://unpkg.com/react-dom@18/umd/react-dom.production.min.js"" crossorigin></script>
    <script src=""https://unpkg.com/@babel/standalone/babel.min.js""></script>
</head>
<body>
    <div id=""root""></div>
    <script type=""text/babel"" data-type=""module"">
        {GetEmbeddedResource("YarpAdmin.app.jsx")}

        const root = ReactDOM.createRoot(document.getElementById('root'));
        root.render(<YarpAdminDashboard />);
    </script>
</body>
</html>";
    }

    private static string GetEmbeddedResource(string resourceName)
    {
        var assembly = typeof(YarpAdminExtensions).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return "// Resource not found";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

/// <summary>
/// Configuration options for YARP Admin.
/// </summary>
public class YarpAdminOptions
{
    /// <summary>
    /// The title displayed in the admin UI.
    /// </summary>
    public string Title { get; set; } = "YARP Admin";

    /// <summary>
    /// Whether to require authentication to access the admin UI.
    /// </summary>
    public bool RequireAuthentication { get; set; } = false;

    /// <summary>
    /// The authentication policy name to use when RequireAuthentication is true.
    /// </summary>
    public string? AuthenticationPolicy { get; set; }

    /// <summary>
    /// Whether to allow configuration changes (set to false for read-only mode).
    /// </summary>
    public bool AllowConfigurationChanges { get; set; } = true;

    /// <summary>
    /// Path to persist configuration (optional).
    /// </summary>
    public string? ConfigurationFilePath { get; set; }
}
