using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yarp.ReverseProxy.Configuration;
using YarpAdmin;

namespace YarpAdmin.Tests;

public class YarpAdminExtensionsTests
{
    #region AddYarpAdmin Tests

    [Fact]
    public void AddYarpAdmin_RegistersRequiredServices()
    {
        var services = new ServiceCollection();

        services.AddYarpAdmin();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<YarpAdminOptions>());
        Assert.NotNull(provider.GetService<IYarpConfigurationStore>());
        Assert.NotNull(provider.GetService<IYarpAdminService>());
        Assert.NotNull(provider.GetService<IProxyConfigProvider>());
    }

    [Fact]
    public void AddYarpAdmin_WithConfiguration_AppliesOptions()
    {
        var services = new ServiceCollection();

        services.AddYarpAdmin(options =>
        {
            options.Title = "Custom Title";
            options.RequireAuthentication = true;
            options.AuthenticationPolicy = "AdminPolicy";
            options.AllowConfigurationChanges = false;
            options.ConfigurationFilePath = "/path/to/config.json";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<YarpAdminOptions>();

        Assert.Equal("Custom Title", options.Title);
        Assert.True(options.RequireAuthentication);
        Assert.Equal("AdminPolicy", options.AuthenticationPolicy);
        Assert.False(options.AllowConfigurationChanges);
        Assert.Equal("/path/to/config.json", options.ConfigurationFilePath);
    }

    [Fact]
    public void AddYarpAdmin_RegistersInMemoryConfigurationStore()
    {
        var services = new ServiceCollection();

        services.AddYarpAdmin();

        var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IYarpConfigurationStore>();

        Assert.IsType<InMemoryYarpConfigurationStore>(store);
    }

    [Fact]
    public void AddYarpAdmin_RegistersYarpAdminServiceAsProxyConfigProvider()
    {
        var services = new ServiceCollection();

        services.AddYarpAdmin();

        var provider = services.BuildServiceProvider();
        var adminService = provider.GetRequiredService<IYarpAdminService>();
        var configProvider = provider.GetRequiredService<IProxyConfigProvider>();

        Assert.Same(adminService, configProvider);
    }

    [Fact]
    public void AddYarpAdmin_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddYarpAdmin();

        Assert.Same(services, result);
    }

    #endregion

    #region AddYarpAdmin<TStore> Tests

    [Fact]
    public void AddYarpAdminWithCustomStore_RegistersCustomStore()
    {
        var services = new ServiceCollection();

        services.AddYarpAdmin<TestConfigurationStore>();

        var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IYarpConfigurationStore>();

        Assert.IsType<TestConfigurationStore>(store);
    }

    [Fact]
    public void AddYarpAdminWithCustomStore_WithConfiguration_AppliesOptions()
    {
        var services = new ServiceCollection();

        services.AddYarpAdmin<TestConfigurationStore>(options =>
        {
            options.Title = "Custom Store Admin";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<YarpAdminOptions>();

        Assert.Equal("Custom Store Admin", options.Title);
    }

    #endregion

    #region MapYarpAdmin Integration Tests

    [Fact]
    public async Task MapYarpAdmin_ServesAdminUI()
    {
        using var host = await CreateTestHost();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/yarp-admin");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("<!DOCTYPE html>", content);
        Assert.Contains("YARP Admin", content);
    }

    [Fact]
    public async Task MapYarpAdmin_ServesApiEndpoints()
    {
        using var host = await CreateTestHost();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/api/yarp-admin/routes");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("[]", content);
    }

    [Fact]
    public async Task MapYarpAdmin_WithCustomPrefix_ServesAtCustomPath()
    {
        using var host = await CreateTestHost("/custom-admin");
        var client = host.GetTestClient();

        var response = await client.GetAsync("/custom-admin");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("<!DOCTYPE html>", content);
    }

    [Fact]
    public async Task MapYarpAdmin_ServesAppJsx()
    {
        using var host = await CreateTestHost();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/yarp-admin/app.jsx");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/javascript", response.Content.Headers.ContentType?.MediaType);
    }

    #endregion

    #region Helper Methods

    private static async Task<IHost> CreateTestHost(string pathPrefix = "/yarp-admin")
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddYarpAdmin(options =>
                    {
                        options.Title = "YARP Admin";
                    });
                    services.AddReverseProxy();
                    services.AddRouting();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapYarpAdmin(pathPrefix);
                    });
                });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    #endregion

    #region Test Doubles

    private class TestConfigurationStore : IYarpConfigurationStore
    {
        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        public Task<bool> DeleteClusterAsync(string clusterId) => Task.FromResult(false);
        public Task<bool> DeleteRouteAsync(string routeId) => Task.FromResult(false);
        public Task<Models.ClusterConfig?> GetClusterAsync(string clusterId) => Task.FromResult<Models.ClusterConfig?>(null);
        public Task<IEnumerable<Models.ClusterConfig>> GetClustersAsync() => Task.FromResult<IEnumerable<Models.ClusterConfig>>(Array.Empty<Models.ClusterConfig>());
        public Task<Models.YarpConfiguration> GetConfigurationAsync() => Task.FromResult(new Models.YarpConfiguration());
        public Task<Models.RouteConfig?> GetRouteAsync(string routeId) => Task.FromResult<Models.RouteConfig?>(null);
        public Task<IEnumerable<Models.RouteConfig>> GetRoutesAsync() => Task.FromResult<IEnumerable<Models.RouteConfig>>(Array.Empty<Models.RouteConfig>());
        public Task LoadAsync() => Task.CompletedTask;
        public Task SaveAsync() => Task.CompletedTask;
        public Task<Models.ClusterConfig> UpsertClusterAsync(Models.ClusterConfig cluster) => Task.FromResult(cluster);
        public Task<Models.RouteConfig> UpsertRouteAsync(Models.RouteConfig route) => Task.FromResult(route);

        protected virtual void OnConfigurationChanged(ConfigurationChangedEventArgs e)
        {
            ConfigurationChanged?.Invoke(this, e);
        }
    }

    #endregion
}
