using YarpAdmin;
using YarpAdmin.Models;

namespace YarpAdmin.Tests;

public class InMemoryYarpConfigurationStoreTests : IDisposable
{
    private readonly string _tempConfigPath;
    private readonly YarpAdminOptions _options;
    private readonly InMemoryYarpConfigurationStore _store;

    public InMemoryYarpConfigurationStoreTests()
    {
        _tempConfigPath = Path.Combine(Path.GetTempPath(), $"yarp-test-{Guid.NewGuid()}.json");
        _options = new YarpAdminOptions();
        _store = new InMemoryYarpConfigurationStore(_options);
    }

    public void Dispose()
    {
        if (File.Exists(_tempConfigPath))
        {
            File.Delete(_tempConfigPath);
        }
    }

    #region Route Tests

    [Fact]
    public async Task GetRoutesAsync_EmptyStore_ReturnsEmptyCollection()
    {
        var routes = await _store.GetRoutesAsync();
        Assert.Empty(routes);
    }

    [Fact]
    public async Task UpsertRouteAsync_NewRoute_AddsRoute()
    {
        var route = CreateTestRoute("test-route");

        var result = await _store.UpsertRouteAsync(route);

        Assert.Equal("test-route", result.RouteId);
        var routes = await _store.GetRoutesAsync();
        Assert.Single(routes);
    }

    [Fact]
    public async Task UpsertRouteAsync_ExistingRoute_UpdatesRoute()
    {
        var route = CreateTestRoute("test-route");
        await _store.UpsertRouteAsync(route);

        route.ClusterId = "updated-cluster";
        var result = await _store.UpsertRouteAsync(route);

        Assert.Equal("updated-cluster", result.ClusterId);
        var routes = await _store.GetRoutesAsync();
        Assert.Single(routes);
    }

    [Fact]
    public async Task GetRouteAsync_ExistingRoute_ReturnsRoute()
    {
        var route = CreateTestRoute("test-route");
        await _store.UpsertRouteAsync(route);

        var result = await _store.GetRouteAsync("test-route");

        Assert.NotNull(result);
        Assert.Equal("test-route", result.RouteId);
    }

    [Fact]
    public async Task GetRouteAsync_NonExistingRoute_ReturnsNull()
    {
        var result = await _store.GetRouteAsync("non-existing");
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteRouteAsync_ExistingRoute_ReturnsTrue()
    {
        var route = CreateTestRoute("test-route");
        await _store.UpsertRouteAsync(route);

        var result = await _store.DeleteRouteAsync("test-route");

        Assert.True(result);
        var routes = await _store.GetRoutesAsync();
        Assert.Empty(routes);
    }

    [Fact]
    public async Task DeleteRouteAsync_NonExistingRoute_ReturnsFalse()
    {
        var result = await _store.DeleteRouteAsync("non-existing");
        Assert.False(result);
    }

    #endregion

    #region Cluster Tests

    [Fact]
    public async Task GetClustersAsync_EmptyStore_ReturnsEmptyCollection()
    {
        var clusters = await _store.GetClustersAsync();
        Assert.Empty(clusters);
    }

    [Fact]
    public async Task UpsertClusterAsync_NewCluster_AddsCluster()
    {
        var cluster = CreateTestCluster("test-cluster");

        var result = await _store.UpsertClusterAsync(cluster);

        Assert.Equal("test-cluster", result.ClusterId);
        var clusters = await _store.GetClustersAsync();
        Assert.Single(clusters);
    }

    [Fact]
    public async Task UpsertClusterAsync_ExistingCluster_UpdatesCluster()
    {
        var cluster = CreateTestCluster("test-cluster");
        await _store.UpsertClusterAsync(cluster);

        cluster.LoadBalancingPolicy = "Random";
        var result = await _store.UpsertClusterAsync(cluster);

        Assert.Equal("Random", result.LoadBalancingPolicy);
        var clusters = await _store.GetClustersAsync();
        Assert.Single(clusters);
    }

    [Fact]
    public async Task GetClusterAsync_ExistingCluster_ReturnsCluster()
    {
        var cluster = CreateTestCluster("test-cluster");
        await _store.UpsertClusterAsync(cluster);

        var result = await _store.GetClusterAsync("test-cluster");

        Assert.NotNull(result);
        Assert.Equal("test-cluster", result.ClusterId);
    }

    [Fact]
    public async Task GetClusterAsync_NonExistingCluster_ReturnsNull()
    {
        var result = await _store.GetClusterAsync("non-existing");
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteClusterAsync_ExistingCluster_ReturnsTrue()
    {
        var cluster = CreateTestCluster("test-cluster");
        await _store.UpsertClusterAsync(cluster);

        var result = await _store.DeleteClusterAsync("test-cluster");

        Assert.True(result);
        var clusters = await _store.GetClustersAsync();
        Assert.Empty(clusters);
    }

    [Fact]
    public async Task DeleteClusterAsync_NonExistingCluster_ReturnsFalse()
    {
        var result = await _store.DeleteClusterAsync("non-existing");
        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task GetConfigurationAsync_ReturnsCompleteConfiguration()
    {
        await _store.UpsertRouteAsync(CreateTestRoute("route-1"));
        await _store.UpsertRouteAsync(CreateTestRoute("route-2"));
        await _store.UpsertClusterAsync(CreateTestCluster("cluster-1"));

        var config = await _store.GetConfigurationAsync();

        Assert.Equal(2, config.Routes.Count);
        Assert.Single(config.Clusters);
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public async Task SaveAsync_WithConfigurationFilePath_SavesConfiguration()
    {
        var options = new YarpAdminOptions { ConfigurationFilePath = _tempConfigPath };
        var store = new InMemoryYarpConfigurationStore(options);

        await store.UpsertRouteAsync(CreateTestRoute("test-route"));
        await store.UpsertClusterAsync(CreateTestCluster("test-cluster"));

        Assert.True(File.Exists(_tempConfigPath));
        var content = await File.ReadAllTextAsync(_tempConfigPath);
        Assert.Contains("test-route", content);
        Assert.Contains("test-cluster", content);
    }

    [Fact]
    public async Task LoadAsync_WithExistingFile_LoadsConfiguration()
    {
        var json = @"{
            ""routes"": [{""routeId"": ""loaded-route"", ""clusterId"": ""cluster-1""}],
            ""clusters"": [{""clusterId"": ""loaded-cluster""}]
        }";
        await File.WriteAllTextAsync(_tempConfigPath, json);

        var options = new YarpAdminOptions { ConfigurationFilePath = _tempConfigPath };
        var store = new InMemoryYarpConfigurationStore(options);

        // Wait for async load to complete
        await Task.Delay(100);

        var routes = await store.GetRoutesAsync();
        var clusters = await store.GetClustersAsync();

        Assert.Single(routes);
        Assert.Single(clusters);
    }

    [Fact]
    public async Task SaveAsync_WithoutConfigurationFilePath_DoesNothing()
    {
        var options = new YarpAdminOptions { ConfigurationFilePath = null };
        var store = new InMemoryYarpConfigurationStore(options);

        await store.UpsertRouteAsync(CreateTestRoute("test-route"));

        // Should not throw and should not create any file
        await store.SaveAsync();
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task UpsertRouteAsync_NewRoute_RaisesConfigurationChangedEvent()
    {
        ConfigurationChangedEventArgs? eventArgs = null;
        _store.ConfigurationChanged += (_, args) => eventArgs = args;

        await _store.UpsertRouteAsync(CreateTestRoute("test-route"));

        Assert.NotNull(eventArgs);
        Assert.Equal(ChangeType.Added, eventArgs.ChangeType);
        Assert.Equal("Route", eventArgs.EntityType);
        Assert.Equal("test-route", eventArgs.EntityId);
    }

    [Fact]
    public async Task UpsertRouteAsync_ExistingRoute_RaisesUpdatedEvent()
    {
        await _store.UpsertRouteAsync(CreateTestRoute("test-route"));

        ConfigurationChangedEventArgs? eventArgs = null;
        _store.ConfigurationChanged += (_, args) => eventArgs = args;

        await _store.UpsertRouteAsync(CreateTestRoute("test-route"));

        Assert.NotNull(eventArgs);
        Assert.Equal(ChangeType.Updated, eventArgs.ChangeType);
    }

    [Fact]
    public async Task DeleteRouteAsync_ExistingRoute_RaisesDeletedEvent()
    {
        await _store.UpsertRouteAsync(CreateTestRoute("test-route"));

        ConfigurationChangedEventArgs? eventArgs = null;
        _store.ConfigurationChanged += (_, args) => eventArgs = args;

        await _store.DeleteRouteAsync("test-route");

        Assert.NotNull(eventArgs);
        Assert.Equal(ChangeType.Deleted, eventArgs.ChangeType);
        Assert.Equal("Route", eventArgs.EntityType);
        Assert.Equal("test-route", eventArgs.EntityId);
    }

    [Fact]
    public async Task UpsertClusterAsync_NewCluster_RaisesConfigurationChangedEvent()
    {
        ConfigurationChangedEventArgs? eventArgs = null;
        _store.ConfigurationChanged += (_, args) => eventArgs = args;

        await _store.UpsertClusterAsync(CreateTestCluster("test-cluster"));

        Assert.NotNull(eventArgs);
        Assert.Equal(ChangeType.Added, eventArgs.ChangeType);
        Assert.Equal("Cluster", eventArgs.EntityType);
        Assert.Equal("test-cluster", eventArgs.EntityId);
    }

    [Fact]
    public async Task DeleteClusterAsync_ExistingCluster_RaisesDeletedEvent()
    {
        await _store.UpsertClusterAsync(CreateTestCluster("test-cluster"));

        ConfigurationChangedEventArgs? eventArgs = null;
        _store.ConfigurationChanged += (_, args) => eventArgs = args;

        await _store.DeleteClusterAsync("test-cluster");

        Assert.NotNull(eventArgs);
        Assert.Equal(ChangeType.Deleted, eventArgs.ChangeType);
        Assert.Equal("Cluster", eventArgs.EntityType);
        Assert.Equal("test-cluster", eventArgs.EntityId);
    }

    #endregion

    #region Helper Methods

    private static RouteConfig CreateTestRoute(string routeId, string clusterId = "test-cluster")
    {
        return new RouteConfig
        {
            RouteId = routeId,
            ClusterId = clusterId,
            Match = new RouteMatch
            {
                Path = "/api/{**catch-all}"
            },
            Enabled = true
        };
    }

    private static ClusterConfig CreateTestCluster(string clusterId)
    {
        return new ClusterConfig
        {
            ClusterId = clusterId,
            LoadBalancingPolicy = "RoundRobin",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["dest-1"] = new DestinationConfig { Address = "https://localhost:5001" }
            }
        };
    }

    #endregion
}
