using YarpAdmin;
using YarpAdmin.Models;

namespace YarpAdmin.Tests;

public class YarpAdminServiceTests
{
    private readonly YarpAdminOptions _options;
    private readonly InMemoryYarpConfigurationStore _store;
    private readonly YarpAdminService _service;

    public YarpAdminServiceTests()
    {
        _options = new YarpAdminOptions();
        _store = new InMemoryYarpConfigurationStore(_options);
        _service = new YarpAdminService(_store, _options);
    }

    #region Route Delegation Tests

    [Fact]
    public async Task GetRoutesAsync_DelegatesToStore()
    {
        await _store.UpsertRouteAsync(CreateTestRoute("route-1"));
        await _store.UpsertRouteAsync(CreateTestRoute("route-2"));

        var routes = await _service.GetRoutesAsync();

        Assert.Equal(2, routes.Count());
    }

    [Fact]
    public async Task GetRouteAsync_DelegatesToStore()
    {
        await _store.UpsertRouteAsync(CreateTestRoute("test-route"));

        var route = await _service.GetRouteAsync("test-route");

        Assert.NotNull(route);
        Assert.Equal("test-route", route.RouteId);
    }

    [Fact]
    public async Task UpsertRouteAsync_DelegatesToStore()
    {
        var route = CreateTestRoute("test-route");

        var result = await _service.UpsertRouteAsync(route);

        Assert.Equal("test-route", result.RouteId);
        var stored = await _store.GetRouteAsync("test-route");
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task DeleteRouteAsync_DelegatesToStore()
    {
        await _store.UpsertRouteAsync(CreateTestRoute("test-route"));

        var result = await _service.DeleteRouteAsync("test-route");

        Assert.True(result);
        var stored = await _store.GetRouteAsync("test-route");
        Assert.Null(stored);
    }

    #endregion

    #region Cluster Delegation Tests

    [Fact]
    public async Task GetClustersAsync_DelegatesToStore()
    {
        await _store.UpsertClusterAsync(CreateTestCluster("cluster-1"));
        await _store.UpsertClusterAsync(CreateTestCluster("cluster-2"));

        var clusters = await _service.GetClustersAsync();

        Assert.Equal(2, clusters.Count());
    }

    [Fact]
    public async Task GetClusterAsync_DelegatesToStore()
    {
        await _store.UpsertClusterAsync(CreateTestCluster("test-cluster"));

        var cluster = await _service.GetClusterAsync("test-cluster");

        Assert.NotNull(cluster);
        Assert.Equal("test-cluster", cluster.ClusterId);
    }

    [Fact]
    public async Task UpsertClusterAsync_DelegatesToStore()
    {
        var cluster = CreateTestCluster("test-cluster");

        var result = await _service.UpsertClusterAsync(cluster);

        Assert.Equal("test-cluster", result.ClusterId);
        var stored = await _store.GetClusterAsync("test-cluster");
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task DeleteClusterAsync_DelegatesToStore()
    {
        await _store.UpsertClusterAsync(CreateTestCluster("test-cluster"));

        var result = await _service.DeleteClusterAsync("test-cluster");

        Assert.True(result);
        var stored = await _store.GetClusterAsync("test-cluster");
        Assert.Null(stored);
    }

    #endregion

    #region IProxyConfigProvider Tests

    [Fact]
    public async Task GetConfig_ReturnsProxyConfig()
    {
        await _store.UpsertRouteAsync(CreateTestRoute("route-1"));
        await _store.UpsertClusterAsync(CreateTestCluster("cluster-1"));
        await _service.ApplyConfigurationAsync();

        var config = _service.GetConfig();

        Assert.NotNull(config);
        Assert.NotNull(config.Routes);
        Assert.NotNull(config.Clusters);
    }

    [Fact]
    public async Task ApplyConfigurationAsync_UpdatesProxyConfig()
    {
        var initialConfig = _service.GetConfig();
        Assert.Empty(initialConfig.Routes);

        await _store.UpsertRouteAsync(CreateTestRoute("route-1", enabled: true));
        await _service.ApplyConfigurationAsync();

        var updatedConfig = _service.GetConfig();
        Assert.Single(updatedConfig.Routes);
    }

    [Fact]
    public async Task ApplyConfigurationAsync_OnlyIncludesEnabledRoutes()
    {
        await _store.UpsertRouteAsync(CreateTestRoute("enabled-route", enabled: true));
        await _store.UpsertRouteAsync(CreateTestRoute("disabled-route", enabled: false));
        await _service.ApplyConfigurationAsync();

        var config = _service.GetConfig();

        Assert.Single(config.Routes);
        Assert.Equal("enabled-route", config.Routes[0].RouteId);
    }

    [Fact]
    public async Task GetConfig_HasChangeToken()
    {
        var config = _service.GetConfig();

        Assert.NotNull(config.ChangeToken);
    }

    #endregion

    #region Route Conversion Tests

    [Fact]
    public async Task ApplyConfigurationAsync_ConvertsRouteMatch()
    {
        var route = new RouteConfig
        {
            RouteId = "test-route",
            ClusterId = "test-cluster",
            Enabled = true,
            Match = new RouteMatch
            {
                Path = "/api/{**catch-all}",
                Methods = new List<string> { "GET", "POST" },
                Hosts = new List<string> { "example.com" }
            }
        };
        await _store.UpsertRouteAsync(route);
        await _service.ApplyConfigurationAsync();

        var config = _service.GetConfig();

        Assert.Single(config.Routes);
        var yarpRoute = config.Routes[0];
        Assert.Equal("/api/{**catch-all}", yarpRoute.Match.Path);
        Assert.Equal(2, yarpRoute.Match.Methods?.Count);
        Assert.Single(yarpRoute.Match.Hosts ?? new List<string>());
    }

    [Fact]
    public async Task ApplyConfigurationAsync_ConvertsRouteHeaders()
    {
        var route = new RouteConfig
        {
            RouteId = "test-route",
            ClusterId = "test-cluster",
            Enabled = true,
            Match = new RouteMatch
            {
                Path = "/api",
                Headers = new List<RouteHeader>
                {
                    new RouteHeader
                    {
                        Name = "X-Custom-Header",
                        Values = new List<string> { "value1", "value2" },
                        Mode = "ExactHeader",
                        IsCaseSensitive = true
                    }
                }
            }
        };
        await _store.UpsertRouteAsync(route);
        await _service.ApplyConfigurationAsync();

        var config = _service.GetConfig();

        Assert.Single(config.Routes);
        var yarpRoute = config.Routes[0];
        Assert.NotNull(yarpRoute.Match.Headers);
        Assert.Single(yarpRoute.Match.Headers);
        Assert.Equal("X-Custom-Header", yarpRoute.Match.Headers[0].Name);
    }

    [Fact]
    public async Task ApplyConfigurationAsync_ConvertsRouteQueryParameters()
    {
        var route = new RouteConfig
        {
            RouteId = "test-route",
            ClusterId = "test-cluster",
            Enabled = true,
            Match = new RouteMatch
            {
                Path = "/api",
                QueryParameters = new List<RouteQueryParameter>
                {
                    new RouteQueryParameter
                    {
                        Name = "version",
                        Values = new List<string> { "v1", "v2" },
                        Mode = "Exact",
                        IsCaseSensitive = false
                    }
                }
            }
        };
        await _store.UpsertRouteAsync(route);
        await _service.ApplyConfigurationAsync();

        var config = _service.GetConfig();

        Assert.Single(config.Routes);
        var yarpRoute = config.Routes[0];
        Assert.NotNull(yarpRoute.Match.QueryParameters);
        Assert.Single(yarpRoute.Match.QueryParameters);
        Assert.Equal("version", yarpRoute.Match.QueryParameters[0].Name);
    }

    [Fact]
    public async Task ApplyConfigurationAsync_ConvertsRoutePolicies()
    {
        var route = new RouteConfig
        {
            RouteId = "test-route",
            ClusterId = "test-cluster",
            Enabled = true,
            Match = new RouteMatch { Path = "/api" },
            AuthorizationPolicy = "AdminOnly",
            CorsPolicy = "AllowAll",
            RateLimiterPolicy = "Fixed",
            TimeoutPolicy = "LongRunning",
            Order = 10
        };
        await _store.UpsertRouteAsync(route);
        await _service.ApplyConfigurationAsync();

        var config = _service.GetConfig();

        var yarpRoute = config.Routes[0];
        Assert.Equal("AdminOnly", yarpRoute.AuthorizationPolicy);
        Assert.Equal("AllowAll", yarpRoute.CorsPolicy);
        Assert.Equal("Fixed", yarpRoute.RateLimiterPolicy);
        Assert.Equal("LongRunning", yarpRoute.TimeoutPolicy);
        Assert.Equal(10, yarpRoute.Order);
    }

    [Fact]
    public async Task ApplyConfigurationAsync_ConvertsRouteMetadata()
    {
        var route = new RouteConfig
        {
            RouteId = "test-route",
            ClusterId = "test-cluster",
            Enabled = true,
            Match = new RouteMatch { Path = "/api" },
            Metadata = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }
        };
        await _store.UpsertRouteAsync(route);
        await _service.ApplyConfigurationAsync();

        var config = _service.GetConfig();

        var yarpRoute = config.Routes[0];
        Assert.NotNull(yarpRoute.Metadata);
        Assert.Equal(2, yarpRoute.Metadata.Count);
        Assert.Equal("value1", yarpRoute.Metadata["key1"]);
    }

    #endregion

    #region Cluster Conversion Tests

    [Fact]
    public async Task ApplyConfigurationAsync_ConvertsClusterDestinations()
    {
        var cluster = new ClusterConfig
        {
            ClusterId = "test-cluster",
            LoadBalancingPolicy = "RoundRobin",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["dest-1"] = new DestinationConfig
                {
                    Address = "https://server1.example.com",
                    Health = "https://server1.example.com/health",
                    Metadata = new Dictionary<string, string> { ["region"] = "us-west" }
                },
                ["dest-2"] = new DestinationConfig
                {
                    Address = "https://server2.example.com"
                }
            }
        };
        await _store.UpsertClusterAsync(cluster);
        await _service.ApplyConfigurationAsync();

        var config = _service.GetConfig();

        Assert.Single(config.Clusters);
        var yarpCluster = config.Clusters[0];
        Assert.Equal(2, yarpCluster.Destinations?.Count);
        Assert.Equal("https://server1.example.com", yarpCluster.Destinations?["dest-1"].Address);
        Assert.Equal("https://server1.example.com/health", yarpCluster.Destinations?["dest-1"].Health);
    }

    [Fact]
    public async Task ApplyConfigurationAsync_ConvertsClusterHealthCheck()
    {
        var cluster = new ClusterConfig
        {
            ClusterId = "test-cluster",
            HealthCheck = new HealthCheckConfig
            {
                AvailableDestinationsPolicy = "HealthyAndUnknown",
                Active = new ActiveHealthCheckConfig
                {
                    Enabled = true,
                    Interval = "00:00:30",
                    Timeout = "00:00:10",
                    Policy = "ConsecutiveFailures",
                    Path = "/health"
                },
                Passive = new PassiveHealthCheckConfig
                {
                    Enabled = true,
                    Policy = "TransportFailureRate",
                    ReactivationPeriod = "00:05:00"
                }
            },
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["dest-1"] = new DestinationConfig { Address = "https://server1.example.com" }
            }
        };
        await _store.UpsertClusterAsync(cluster);
        await _service.ApplyConfigurationAsync();

        var config = _service.GetConfig();

        var yarpCluster = config.Clusters[0];
        Assert.NotNull(yarpCluster.HealthCheck);
        Assert.NotNull(yarpCluster.HealthCheck.Active);
        Assert.True(yarpCluster.HealthCheck.Active.Enabled);
        Assert.Equal("/health", yarpCluster.HealthCheck.Active.Path);
        Assert.NotNull(yarpCluster.HealthCheck.Passive);
        Assert.True(yarpCluster.HealthCheck.Passive.Enabled);
    }

    [Fact]
    public async Task ApplyConfigurationAsync_ConvertsClusterSessionAffinity()
    {
        var cluster = new ClusterConfig
        {
            ClusterId = "test-cluster",
            SessionAffinity = new SessionAffinityConfig
            {
                Enabled = true,
                Policy = "Cookie",
                FailurePolicy = "Redistribute",
                AffinityKeyName = ".MyApp.Affinity"
            },
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["dest-1"] = new DestinationConfig { Address = "https://server1.example.com" }
            }
        };
        await _store.UpsertClusterAsync(cluster);
        await _service.ApplyConfigurationAsync();

        var config = _service.GetConfig();

        var yarpCluster = config.Clusters[0];
        Assert.NotNull(yarpCluster.SessionAffinity);
        Assert.True(yarpCluster.SessionAffinity.Enabled);
        Assert.Equal("Cookie", yarpCluster.SessionAffinity.Policy);
        Assert.Equal("Redistribute", yarpCluster.SessionAffinity.FailurePolicy);
        Assert.Equal(".MyApp.Affinity", yarpCluster.SessionAffinity.AffinityKeyName);
    }

    [Fact]
    public async Task ApplyConfigurationAsync_SessionAffinityUsesDefaultAffinityKeyName()
    {
        var cluster = new ClusterConfig
        {
            ClusterId = "test-cluster",
            SessionAffinity = new SessionAffinityConfig
            {
                Enabled = true,
                Policy = "Cookie",
                AffinityKeyName = null
            },
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["dest-1"] = new DestinationConfig { Address = "https://server1.example.com" }
            }
        };
        await _store.UpsertClusterAsync(cluster);
        await _service.ApplyConfigurationAsync();

        var config = _service.GetConfig();

        var yarpCluster = config.Clusters[0];
        Assert.Equal(".Yarp.Affinity", yarpCluster.SessionAffinity?.AffinityKeyName);
    }

    [Fact]
    public async Task ApplyConfigurationAsync_ConvertsClusterMetadata()
    {
        var cluster = new ClusterConfig
        {
            ClusterId = "test-cluster",
            Metadata = new Dictionary<string, string>
            {
                ["environment"] = "production",
                ["version"] = "2.0"
            },
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["dest-1"] = new DestinationConfig { Address = "https://server1.example.com" }
            }
        };
        await _store.UpsertClusterAsync(cluster);
        await _service.ApplyConfigurationAsync();

        var config = _service.GetConfig();

        var yarpCluster = config.Clusters[0];
        Assert.NotNull(yarpCluster.Metadata);
        Assert.Equal("production", yarpCluster.Metadata["environment"]);
        Assert.Equal("2.0", yarpCluster.Metadata["version"]);
    }

    #endregion

    #region Helper Methods

    private static RouteConfig CreateTestRoute(string routeId, string clusterId = "test-cluster", bool enabled = true)
    {
        return new RouteConfig
        {
            RouteId = routeId,
            ClusterId = clusterId,
            Match = new RouteMatch
            {
                Path = "/api/{**catch-all}"
            },
            Enabled = enabled
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
