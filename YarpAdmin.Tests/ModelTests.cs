using System.Text.Json;
using YarpAdmin;
using YarpAdmin.Models;

namespace YarpAdmin.Tests;

public class ModelTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    #region RouteConfig Tests

    [Fact]
    public void RouteConfig_DefaultValues_AreCorrect()
    {
        var route = new RouteConfig();

        Assert.Equal(string.Empty, route.RouteId);
        Assert.Equal(string.Empty, route.ClusterId);
        Assert.Null(route.Match);
        Assert.Null(route.Order);
        Assert.True(route.Enabled);
        Assert.Null(route.AuthorizationPolicy);
        Assert.Null(route.CorsPolicy);
        Assert.Null(route.RateLimiterPolicy);
        Assert.Null(route.TimeoutPolicy);
        Assert.Null(route.Transforms);
        Assert.Null(route.Metadata);
    }

    [Fact]
    public void RouteConfig_Serialization_RoundTrip()
    {
        var route = new RouteConfig
        {
            RouteId = "test-route",
            ClusterId = "test-cluster",
            Match = new RouteMatch { Path = "/api/{**catch-all}" },
            Order = 10,
            Enabled = true,
            AuthorizationPolicy = "AdminOnly",
            Metadata = new Dictionary<string, string> { ["key"] = "value" }
        };

        var json = JsonSerializer.Serialize(route, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<RouteConfig>(json, _jsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(route.RouteId, deserialized.RouteId);
        Assert.Equal(route.ClusterId, deserialized.ClusterId);
        Assert.Equal(route.Match?.Path, deserialized.Match?.Path);
        Assert.Equal(route.Order, deserialized.Order);
        Assert.Equal(route.Enabled, deserialized.Enabled);
        Assert.Equal(route.AuthorizationPolicy, deserialized.AuthorizationPolicy);
        Assert.Equal("value", deserialized.Metadata?["key"]);
    }

    #endregion

    #region RouteMatch Tests

    [Fact]
    public void RouteMatch_DefaultValues_AreCorrect()
    {
        var match = new RouteMatch();

        Assert.Null(match.Path);
        Assert.Null(match.Methods);
        Assert.Null(match.Hosts);
        Assert.Null(match.Headers);
        Assert.Null(match.QueryParameters);
    }

    [Fact]
    public void RouteMatch_WithHeaders_SerializesCorrectly()
    {
        var match = new RouteMatch
        {
            Path = "/api",
            Headers = new List<RouteHeader>
            {
                new RouteHeader
                {
                    Name = "X-Custom",
                    Values = new List<string> { "value1", "value2" },
                    Mode = "Contains",
                    IsCaseSensitive = true
                }
            }
        };

        var json = JsonSerializer.Serialize(match, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<RouteMatch>(json, _jsonOptions);

        Assert.NotNull(deserialized?.Headers);
        Assert.Single(deserialized.Headers);
        Assert.Equal("X-Custom", deserialized.Headers[0].Name);
        Assert.Equal(2, deserialized.Headers[0].Values?.Count);
        Assert.Equal("Contains", deserialized.Headers[0].Mode);
        Assert.True(deserialized.Headers[0].IsCaseSensitive);
    }

    [Fact]
    public void RouteMatch_WithQueryParameters_SerializesCorrectly()
    {
        var match = new RouteMatch
        {
            Path = "/api",
            QueryParameters = new List<RouteQueryParameter>
            {
                new RouteQueryParameter
                {
                    Name = "version",
                    Values = new List<string> { "v1" },
                    Mode = "Prefix",
                    IsCaseSensitive = false
                }
            }
        };

        var json = JsonSerializer.Serialize(match, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<RouteMatch>(json, _jsonOptions);

        Assert.NotNull(deserialized?.QueryParameters);
        Assert.Single(deserialized.QueryParameters);
        Assert.Equal("version", deserialized.QueryParameters[0].Name);
        Assert.Equal("Prefix", deserialized.QueryParameters[0].Mode);
    }

    #endregion

    #region RouteHeader Tests

    [Fact]
    public void RouteHeader_DefaultValues_AreCorrect()
    {
        var header = new RouteHeader();

        Assert.Equal(string.Empty, header.Name);
        Assert.Null(header.Values);
        Assert.Equal("ExactHeader", header.Mode);
        Assert.False(header.IsCaseSensitive);
    }

    #endregion

    #region RouteQueryParameter Tests

    [Fact]
    public void RouteQueryParameter_DefaultValues_AreCorrect()
    {
        var queryParam = new RouteQueryParameter();

        Assert.Equal(string.Empty, queryParam.Name);
        Assert.Null(queryParam.Values);
        Assert.Equal("Exact", queryParam.Mode);
        Assert.False(queryParam.IsCaseSensitive);
    }

    #endregion

    #region ClusterConfig Tests

    [Fact]
    public void ClusterConfig_DefaultValues_AreCorrect()
    {
        var cluster = new ClusterConfig();

        Assert.Equal(string.Empty, cluster.ClusterId);
        Assert.Null(cluster.LoadBalancingPolicy);
        Assert.Null(cluster.SessionAffinity);
        Assert.Null(cluster.HealthCheck);
        Assert.Null(cluster.HttpClient);
        Assert.Null(cluster.HttpRequest);
        Assert.Null(cluster.Destinations);
        Assert.Null(cluster.Metadata);
    }

    [Fact]
    public void ClusterConfig_Serialization_RoundTrip()
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
                    Health = "https://server1.example.com/health"
                }
            },
            Metadata = new Dictionary<string, string> { ["env"] = "prod" }
        };

        var json = JsonSerializer.Serialize(cluster, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ClusterConfig>(json, _jsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(cluster.ClusterId, deserialized.ClusterId);
        Assert.Equal(cluster.LoadBalancingPolicy, deserialized.LoadBalancingPolicy);
        Assert.Single(deserialized.Destinations!);
        Assert.Equal("https://server1.example.com", deserialized.Destinations!["dest-1"].Address);
    }

    #endregion

    #region DestinationConfig Tests

    [Fact]
    public void DestinationConfig_DefaultValues_AreCorrect()
    {
        var dest = new DestinationConfig();

        Assert.Equal(string.Empty, dest.Address);
        Assert.Null(dest.Health);
        Assert.Null(dest.Metadata);
    }

    #endregion

    #region SessionAffinityConfig Tests

    [Fact]
    public void SessionAffinityConfig_DefaultValues_AreCorrect()
    {
        var affinity = new SessionAffinityConfig();

        Assert.False(affinity.Enabled);
        Assert.Null(affinity.Policy);
        Assert.Null(affinity.FailurePolicy);
        Assert.Null(affinity.AffinityKeyName);
    }

    [Fact]
    public void SessionAffinityConfig_Serialization_RoundTrip()
    {
        var affinity = new SessionAffinityConfig
        {
            Enabled = true,
            Policy = "Cookie",
            FailurePolicy = "Redistribute",
            AffinityKeyName = ".MyApp.Affinity"
        };

        var json = JsonSerializer.Serialize(affinity, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<SessionAffinityConfig>(json, _jsonOptions);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Enabled);
        Assert.Equal("Cookie", deserialized.Policy);
        Assert.Equal("Redistribute", deserialized.FailurePolicy);
        Assert.Equal(".MyApp.Affinity", deserialized.AffinityKeyName);
    }

    #endregion

    #region HealthCheckConfig Tests

    [Fact]
    public void HealthCheckConfig_DefaultValues_AreCorrect()
    {
        var healthCheck = new HealthCheckConfig();

        Assert.Null(healthCheck.Passive);
        Assert.Null(healthCheck.Active);
        Assert.Null(healthCheck.AvailableDestinationsPolicy);
    }

    [Fact]
    public void HealthCheckConfig_WithActiveAndPassive_SerializesCorrectly()
    {
        var healthCheck = new HealthCheckConfig
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
        };

        var json = JsonSerializer.Serialize(healthCheck, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<HealthCheckConfig>(json, _jsonOptions);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.Active);
        Assert.True(deserialized.Active.Enabled);
        Assert.Equal("00:00:30", deserialized.Active.Interval);
        Assert.Equal("/health", deserialized.Active.Path);
        Assert.NotNull(deserialized.Passive);
        Assert.True(deserialized.Passive.Enabled);
        Assert.Equal("TransportFailureRate", deserialized.Passive.Policy);
    }

    #endregion

    #region ActiveHealthCheckConfig Tests

    [Fact]
    public void ActiveHealthCheckConfig_DefaultValues_AreCorrect()
    {
        var active = new ActiveHealthCheckConfig();

        Assert.False(active.Enabled);
        Assert.Null(active.Interval);
        Assert.Null(active.Timeout);
        Assert.Null(active.Policy);
        Assert.Null(active.Path);
    }

    #endregion

    #region PassiveHealthCheckConfig Tests

    [Fact]
    public void PassiveHealthCheckConfig_DefaultValues_AreCorrect()
    {
        var passive = new PassiveHealthCheckConfig();

        Assert.False(passive.Enabled);
        Assert.Null(passive.Policy);
        Assert.Null(passive.ReactivationPeriod);
    }

    #endregion

    #region HttpClientConfig Tests

    [Fact]
    public void HttpClientConfig_DefaultValues_AreCorrect()
    {
        var httpClient = new HttpClientConfig();

        Assert.Null(httpClient.SslProtocols);
        Assert.Null(httpClient.DangerousAcceptAnyServerCertificate);
        Assert.Null(httpClient.MaxConnectionsPerServer);
        Assert.Null(httpClient.EnableMultipleHttp2Connections);
        Assert.Null(httpClient.RequestHeaderEncoding);
        Assert.Null(httpClient.ResponseHeaderEncoding);
    }

    [Fact]
    public void HttpClientConfig_Serialization_RoundTrip()
    {
        var httpClient = new HttpClientConfig
        {
            SslProtocols = new List<string> { "Tls12", "Tls13" },
            DangerousAcceptAnyServerCertificate = false,
            MaxConnectionsPerServer = 100,
            EnableMultipleHttp2Connections = true
        };

        var json = JsonSerializer.Serialize(httpClient, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<HttpClientConfig>(json, _jsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.SslProtocols?.Count);
        Assert.False(deserialized.DangerousAcceptAnyServerCertificate);
        Assert.Equal(100, deserialized.MaxConnectionsPerServer);
        Assert.True(deserialized.EnableMultipleHttp2Connections);
    }

    #endregion

    #region HttpRequestConfig Tests

    [Fact]
    public void HttpRequestConfig_DefaultValues_AreCorrect()
    {
        var httpRequest = new HttpRequestConfig();

        Assert.Null(httpRequest.ActivityTimeout);
        Assert.Null(httpRequest.Version);
        Assert.Null(httpRequest.VersionPolicy);
        Assert.Null(httpRequest.AllowResponseBuffering);
    }

    [Fact]
    public void HttpRequestConfig_Serialization_RoundTrip()
    {
        var httpRequest = new HttpRequestConfig
        {
            ActivityTimeout = "00:02:00",
            Version = "2.0",
            VersionPolicy = "RequestVersionOrHigher",
            AllowResponseBuffering = true
        };

        var json = JsonSerializer.Serialize(httpRequest, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<HttpRequestConfig>(json, _jsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("00:02:00", deserialized.ActivityTimeout);
        Assert.Equal("2.0", deserialized.Version);
        Assert.Equal("RequestVersionOrHigher", deserialized.VersionPolicy);
        Assert.True(deserialized.AllowResponseBuffering);
    }

    #endregion

    #region YarpConfiguration Tests

    [Fact]
    public void YarpConfiguration_DefaultValues_AreCorrect()
    {
        var config = new YarpConfiguration();

        Assert.NotNull(config.Routes);
        Assert.Empty(config.Routes);
        Assert.NotNull(config.Clusters);
        Assert.Empty(config.Clusters);
    }

    [Fact]
    public void YarpConfiguration_Serialization_RoundTrip()
    {
        var config = new YarpConfiguration
        {
            Routes = new List<RouteConfig>
            {
                new RouteConfig
                {
                    RouteId = "route-1",
                    ClusterId = "cluster-1",
                    Match = new RouteMatch { Path = "/api" }
                }
            },
            Clusters = new List<ClusterConfig>
            {
                new ClusterConfig
                {
                    ClusterId = "cluster-1",
                    LoadBalancingPolicy = "RoundRobin"
                }
            }
        };

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<YarpConfiguration>(json, _jsonOptions);

        Assert.NotNull(deserialized);
        Assert.Single(deserialized.Routes);
        Assert.Single(deserialized.Clusters);
        Assert.Equal("route-1", deserialized.Routes[0].RouteId);
        Assert.Equal("cluster-1", deserialized.Clusters[0].ClusterId);
    }

    #endregion

    #region YarpAdminOptions Tests

    [Fact]
    public void YarpAdminOptions_DefaultValues_AreCorrect()
    {
        var options = new YarpAdminOptions();

        Assert.Equal("YARP Admin", options.Title);
        Assert.False(options.RequireAuthentication);
        Assert.Null(options.AuthenticationPolicy);
        Assert.True(options.AllowConfigurationChanges);
        Assert.Null(options.ConfigurationFilePath);
    }

    [Fact]
    public void YarpAdminOptions_CanSetAllProperties()
    {
        var options = new YarpAdminOptions
        {
            Title = "Custom Admin",
            RequireAuthentication = true,
            AuthenticationPolicy = "AdminOnly",
            AllowConfigurationChanges = false,
            ConfigurationFilePath = "/path/to/config.json"
        };

        Assert.Equal("Custom Admin", options.Title);
        Assert.True(options.RequireAuthentication);
        Assert.Equal("AdminOnly", options.AuthenticationPolicy);
        Assert.False(options.AllowConfigurationChanges);
        Assert.Equal("/path/to/config.json", options.ConfigurationFilePath);
    }

    #endregion

    #region ConfigurationChangedEventArgs Tests

    [Fact]
    public void ConfigurationChangedEventArgs_CanSetAllProperties()
    {
        var args = new ConfigurationChangedEventArgs
        {
            ChangeType = ChangeType.Updated,
            EntityType = "Route",
            EntityId = "test-route"
        };

        Assert.Equal(ChangeType.Updated, args.ChangeType);
        Assert.Equal("Route", args.EntityType);
        Assert.Equal("test-route", args.EntityId);
    }

    [Fact]
    public void ChangeType_HasAllExpectedValues()
    {
        Assert.Equal(0, (int)ChangeType.Added);
        Assert.Equal(1, (int)ChangeType.Updated);
        Assert.Equal(2, (int)ChangeType.Deleted);
        Assert.Equal(3, (int)ChangeType.Reloaded);
    }

    #endregion
}
