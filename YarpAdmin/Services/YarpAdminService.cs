using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;
using YarpAdmin.Models;
using YarpRouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;
using YarpClusterConfig = Yarp.ReverseProxy.Configuration.ClusterConfig;
using YarpDestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;
using YarpRouteMatch = Yarp.ReverseProxy.Configuration.RouteMatch;
using YarpRouteHeader = Yarp.ReverseProxy.Configuration.RouteHeader;
using YarpRouteQueryParameter = Yarp.ReverseProxy.Configuration.RouteQueryParameter;
using YarpHealthCheckConfig = Yarp.ReverseProxy.Configuration.HealthCheckConfig;
using YarpPassiveHealthCheckConfig = Yarp.ReverseProxy.Configuration.PassiveHealthCheckConfig;
using YarpActiveHealthCheckConfig = Yarp.ReverseProxy.Configuration.ActiveHealthCheckConfig;
using YarpSessionAffinityConfig = Yarp.ReverseProxy.Configuration.SessionAffinityConfig;

namespace YarpAdmin;

/// <summary>
/// Interface for the YARP Admin service.
/// </summary>
public interface IYarpAdminService
{
    /// <summary>
    /// Gets all routes.
    /// </summary>
    Task<IEnumerable<Models.RouteConfig>> GetRoutesAsync();

    /// <summary>
    /// Gets a route by ID.
    /// </summary>
    Task<Models.RouteConfig?> GetRouteAsync(string routeId);

    /// <summary>
    /// Creates or updates a route.
    /// </summary>
    Task<Models.RouteConfig> UpsertRouteAsync(Models.RouteConfig route);

    /// <summary>
    /// Deletes a route.
    /// </summary>
    Task<bool> DeleteRouteAsync(string routeId);

    /// <summary>
    /// Gets all clusters.
    /// </summary>
    Task<IEnumerable<Models.ClusterConfig>> GetClustersAsync();

    /// <summary>
    /// Gets a cluster by ID.
    /// </summary>
    Task<Models.ClusterConfig?> GetClusterAsync(string clusterId);

    /// <summary>
    /// Creates or updates a cluster.
    /// </summary>
    Task<Models.ClusterConfig> UpsertClusterAsync(Models.ClusterConfig cluster);

    /// <summary>
    /// Deletes a cluster.
    /// </summary>
    Task<bool> DeleteClusterAsync(string clusterId);

    /// <summary>
    /// Applies the current configuration to YARP.
    /// </summary>
    Task ApplyConfigurationAsync();
}

/// <summary>
/// Implementation of the YARP Admin service.
/// </summary>
public class YarpAdminService : IYarpAdminService, IProxyConfigProvider
{
    private readonly IYarpConfigurationStore _store;
    private readonly YarpAdminOptions _options;
    private volatile InMemoryConfigProvider _currentConfig;
    private readonly object _syncLock = new();

    public YarpAdminService(IYarpConfigurationStore store, YarpAdminOptions options)
    {
        _store = store;
        _options = options;
        _currentConfig = new InMemoryConfigProvider(Array.Empty<YarpRouteConfig>(), Array.Empty<YarpClusterConfig>());
        
        // Subscribe to configuration changes
        _store.ConfigurationChanged += async (_, _) => await ApplyConfigurationAsync();
        
        // Initial load
        _ = ApplyConfigurationAsync();
    }

    public Task<IEnumerable<Models.RouteConfig>> GetRoutesAsync() => _store.GetRoutesAsync();
    public Task<Models.RouteConfig?> GetRouteAsync(string routeId) => _store.GetRouteAsync(routeId);
    public Task<Models.RouteConfig> UpsertRouteAsync(Models.RouteConfig route) => _store.UpsertRouteAsync(route);
    public Task<bool> DeleteRouteAsync(string routeId) => _store.DeleteRouteAsync(routeId);
    public Task<IEnumerable<Models.ClusterConfig>> GetClustersAsync() => _store.GetClustersAsync();
    public Task<Models.ClusterConfig?> GetClusterAsync(string clusterId) => _store.GetClusterAsync(clusterId);
    public Task<Models.ClusterConfig> UpsertClusterAsync(Models.ClusterConfig cluster) => _store.UpsertClusterAsync(cluster);
    public Task<bool> DeleteClusterAsync(string clusterId) => _store.DeleteClusterAsync(clusterId);

    public async Task ApplyConfigurationAsync()
    {
        var config = await _store.GetConfigurationAsync();
        
        var yarpRoutes = config.Routes
            .Where(r => r.Enabled)
            .Select(ConvertToYarpRoute)
            .ToList();
            
        var yarpClusters = config.Clusters
            .Select(ConvertToYarpCluster)
            .ToList();

        lock (_syncLock)
        {
            var oldConfig = _currentConfig;
            _currentConfig = new InMemoryConfigProvider(yarpRoutes, yarpClusters);
            oldConfig.SignalChange();
        }
    }

    public IProxyConfig GetConfig() => _currentConfig;

    private YarpRouteConfig ConvertToYarpRoute(Models.RouteConfig route)
    {
        var match = new YarpRouteMatch
        {
            Path = route.Match?.Path,
            Methods = route.Match?.Methods,
            Hosts = route.Match?.Hosts,
            Headers = route.Match?.Headers?.Select(h => new YarpRouteHeader
            {
                Name = h.Name,
                Values = h.Values,
                Mode = Enum.TryParse<HeaderMatchMode>(h.Mode, out var mode) ? mode : HeaderMatchMode.ExactHeader,
                IsCaseSensitive = h.IsCaseSensitive
            }).ToList(),
            QueryParameters = route.Match?.QueryParameters?.Select(q => new YarpRouteQueryParameter
            {
                Name = q.Name,
                Values = q.Values,
                Mode = Enum.TryParse<QueryParameterMatchMode>(q.Mode, out var mode) ? mode : QueryParameterMatchMode.Exact,
                IsCaseSensitive = q.IsCaseSensitive
            }).ToList()
        };

        return new YarpRouteConfig
        {
            RouteId = route.RouteId,
            ClusterId = route.ClusterId,
            Match = match,
            Order = route.Order,
            AuthorizationPolicy = route.AuthorizationPolicy,
            CorsPolicy = route.CorsPolicy,
            RateLimiterPolicy = route.RateLimiterPolicy,
            TimeoutPolicy = route.TimeoutPolicy,
            Metadata = route.Metadata
        };
    }

    private YarpClusterConfig ConvertToYarpCluster(Models.ClusterConfig cluster)
    {
        var destinations = cluster.Destinations?.ToDictionary(
            kvp => kvp.Key,
            kvp => new YarpDestinationConfig
            {
                Address = kvp.Value.Address,
                Health = kvp.Value.Health,
                Metadata = kvp.Value.Metadata
            });

        YarpHealthCheckConfig? healthCheck = null;
        if (cluster.HealthCheck != null)
        {
            healthCheck = new YarpHealthCheckConfig
            {
                AvailableDestinationsPolicy = cluster.HealthCheck.AvailableDestinationsPolicy,
                Passive = cluster.HealthCheck.Passive != null ? new YarpPassiveHealthCheckConfig
                {
                    Enabled = cluster.HealthCheck.Passive.Enabled,
                    Policy = cluster.HealthCheck.Passive.Policy,
                    ReactivationPeriod = cluster.HealthCheck.Passive.ReactivationPeriod != null
                        ? TimeSpan.Parse(cluster.HealthCheck.Passive.ReactivationPeriod)
                        : null
                } : null,
                Active = cluster.HealthCheck.Active != null ? new YarpActiveHealthCheckConfig
                {
                    Enabled = cluster.HealthCheck.Active.Enabled,
                    Interval = cluster.HealthCheck.Active.Interval != null
                        ? TimeSpan.Parse(cluster.HealthCheck.Active.Interval)
                        : null,
                    Timeout = cluster.HealthCheck.Active.Timeout != null
                        ? TimeSpan.Parse(cluster.HealthCheck.Active.Timeout)
                        : null,
                    Policy = cluster.HealthCheck.Active.Policy,
                    Path = cluster.HealthCheck.Active.Path
                } : null
            };
        }

        YarpSessionAffinityConfig? sessionAffinity = null;
        if (cluster.SessionAffinity != null)
        {
            sessionAffinity = new YarpSessionAffinityConfig
            {
                Enabled = cluster.SessionAffinity.Enabled,
                Policy = cluster.SessionAffinity.Policy,
                FailurePolicy = cluster.SessionAffinity.FailurePolicy,
                AffinityKeyName = cluster.SessionAffinity.AffinityKeyName ?? ".Yarp.Affinity"
            };
        }

        return new YarpClusterConfig
        {
            ClusterId = cluster.ClusterId,
            LoadBalancingPolicy = cluster.LoadBalancingPolicy,
            SessionAffinity = sessionAffinity,
            HealthCheck = healthCheck,
            Destinations = destinations,
            Metadata = cluster.Metadata
        };
    }

    private class InMemoryConfigProvider : IProxyConfig
    {
        private readonly CancellationTokenSource _cts = new();

        public InMemoryConfigProvider(IReadOnlyList<YarpRouteConfig> routes, IReadOnlyList<YarpClusterConfig> clusters)
        {
            Routes = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(_cts.Token);
        }

        public IReadOnlyList<YarpRouteConfig> Routes { get; }
        public IReadOnlyList<YarpClusterConfig> Clusters { get; }
        public IChangeToken ChangeToken { get; }

        public void SignalChange()
        {
            _cts.Cancel();
        }
    }
}
