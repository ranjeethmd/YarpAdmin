using YarpAdmin;
using YarpAdmin.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// OPTION 1: Basic setup with in-memory store
// ============================================
builder.Services.AddYarpAdmin(options =>
{
    options.Title = "My YARP Admin";
    options.AllowConfigurationChanges = true;
    
    // Optional: Persist configuration to a file
    // options.ConfigurationFilePath = "yarp-config.json";
    
    // Optional: Require authentication
    // options.RequireAuthentication = true;
    // options.AuthenticationPolicy = "YarpAdminPolicy";
});

// Add YARP reverse proxy
builder.Services.AddReverseProxy();

var app = builder.Build();

// Add YARP Admin middleware (optional - for auth/logging)
app.UseYarpAdminAuth();
app.UseYarpAdminLogging();

// Map YARP Admin endpoints and UI
// Access the UI at: /yarp-admin
// Access the API at: /api/yarp-admin/*
app.MapYarpAdmin("/yarp-admin");

// Map YARP reverse proxy with the admin service as config provider
app.MapReverseProxy();

// Seed some example configuration
await SeedExampleConfiguration(app.Services);

app.Run();

// Helper method to seed example configuration
async Task SeedExampleConfiguration(IServiceProvider services)
{
    var adminService = services.GetRequiredService<IYarpAdminService>();
    
    // Check if we already have config
    var existingClusters = await adminService.GetClustersAsync();
    if (existingClusters.Any()) return;
    
    // Add example cluster
    await adminService.UpsertClusterAsync(new YarpAdmin.Models.ClusterConfig
    {
        ClusterId = "api-cluster",
        LoadBalancingPolicy = "RoundRobin",
        Destinations = new Dictionary<string, YarpAdmin.Models.DestinationConfig>
        {
            ["api-1"] = new() { Address = "https://api1.example.com" },
            ["api-2"] = new() { Address = "https://api2.example.com" }
        }
    });
    
    // Add example route
    await adminService.UpsertRouteAsync(new YarpAdmin.Models.RouteConfig
    {
        RouteId = "api-route",
        ClusterId = "api-cluster",
        Match = new YarpAdmin.Models.RouteMatch
        {
            Path = "/api/{**catch-all}"
        }
    });
    
    // Apply the configuration
    await adminService.ApplyConfigurationAsync();
}
