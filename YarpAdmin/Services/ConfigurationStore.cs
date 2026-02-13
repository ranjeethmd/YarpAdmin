using System.Collections.Concurrent;
using System.Text.Json;
using YarpAdmin.Models;

namespace YarpAdmin;

/// <summary>
/// Interface for storing and retrieving YARP configuration.
/// </summary>
public interface IYarpConfigurationStore
{
    /// <summary>
    /// Gets all routes.
    /// </summary>
    Task<IEnumerable<RouteConfig>> GetRoutesAsync();

    /// <summary>
    /// Gets a route by ID.
    /// </summary>
    Task<RouteConfig?> GetRouteAsync(string routeId);

    /// <summary>
    /// Adds or updates a route.
    /// </summary>
    Task<RouteConfig> UpsertRouteAsync(RouteConfig route);

    /// <summary>
    /// Deletes a route.
    /// </summary>
    Task<bool> DeleteRouteAsync(string routeId);

    /// <summary>
    /// Gets all clusters.
    /// </summary>
    Task<IEnumerable<ClusterConfig>> GetClustersAsync();

    /// <summary>
    /// Gets a cluster by ID.
    /// </summary>
    Task<ClusterConfig?> GetClusterAsync(string clusterId);

    /// <summary>
    /// Adds or updates a cluster.
    /// </summary>
    Task<ClusterConfig> UpsertClusterAsync(ClusterConfig cluster);

    /// <summary>
    /// Deletes a cluster.
    /// </summary>
    Task<bool> DeleteClusterAsync(string clusterId);

    /// <summary>
    /// Gets the complete configuration.
    /// </summary>
    Task<YarpConfiguration> GetConfigurationAsync();

    /// <summary>
    /// Saves the configuration to persistent storage (if configured).
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Loads the configuration from persistent storage (if configured).
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Event raised when configuration changes.
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
}

/// <summary>
/// Event args for configuration change events.
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    public ChangeType ChangeType { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
}

public enum ChangeType
{
    Added,
    Updated,
    Deleted,
    Reloaded
}

/// <summary>
/// In-memory implementation of the configuration store.
/// </summary>
public class InMemoryYarpConfigurationStore : IYarpConfigurationStore
{
    private readonly ConcurrentDictionary<string, RouteConfig> _routes = new();
    private readonly ConcurrentDictionary<string, ClusterConfig> _clusters = new();
    private readonly YarpAdminOptions _options;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public InMemoryYarpConfigurationStore(YarpAdminOptions options)
    {
        _options = options;
        
        // Load from file if configured
        if (!string.IsNullOrEmpty(_options.ConfigurationFilePath))
        {
            _ = LoadAsync();
        }
    }

    public Task<IEnumerable<RouteConfig>> GetRoutesAsync()
    {
        return Task.FromResult<IEnumerable<RouteConfig>>(_routes.Values.ToList());
    }

    public Task<RouteConfig?> GetRouteAsync(string routeId)
    {
        _routes.TryGetValue(routeId, out var route);
        return Task.FromResult(route);
    }

    public async Task<RouteConfig> UpsertRouteAsync(RouteConfig route)
    {
        var isUpdate = _routes.ContainsKey(route.RouteId);
        _routes[route.RouteId] = route;
        
        OnConfigurationChanged(new ConfigurationChangedEventArgs
        {
            ChangeType = isUpdate ? ChangeType.Updated : ChangeType.Added,
            EntityType = "Route",
            EntityId = route.RouteId
        });

        if (!string.IsNullOrEmpty(_options.ConfigurationFilePath))
        {
            await SaveAsync();
        }

        return route;
    }

    public async Task<bool> DeleteRouteAsync(string routeId)
    {
        var result = _routes.TryRemove(routeId, out _);
        
        if (result)
        {
            OnConfigurationChanged(new ConfigurationChangedEventArgs
            {
                ChangeType = ChangeType.Deleted,
                EntityType = "Route",
                EntityId = routeId
            });

            if (!string.IsNullOrEmpty(_options.ConfigurationFilePath))
            {
                await SaveAsync();
            }
        }

        return result;
    }

    public Task<IEnumerable<ClusterConfig>> GetClustersAsync()
    {
        return Task.FromResult<IEnumerable<ClusterConfig>>(_clusters.Values.ToList());
    }

    public Task<ClusterConfig?> GetClusterAsync(string clusterId)
    {
        _clusters.TryGetValue(clusterId, out var cluster);
        return Task.FromResult(cluster);
    }

    public async Task<ClusterConfig> UpsertClusterAsync(ClusterConfig cluster)
    {
        var isUpdate = _clusters.ContainsKey(cluster.ClusterId);
        _clusters[cluster.ClusterId] = cluster;
        
        OnConfigurationChanged(new ConfigurationChangedEventArgs
        {
            ChangeType = isUpdate ? ChangeType.Updated : ChangeType.Added,
            EntityType = "Cluster",
            EntityId = cluster.ClusterId
        });

        if (!string.IsNullOrEmpty(_options.ConfigurationFilePath))
        {
            await SaveAsync();
        }

        return cluster;
    }

    public async Task<bool> DeleteClusterAsync(string clusterId)
    {
        var result = _clusters.TryRemove(clusterId, out _);
        
        if (result)
        {
            OnConfigurationChanged(new ConfigurationChangedEventArgs
            {
                ChangeType = ChangeType.Deleted,
                EntityType = "Cluster",
                EntityId = clusterId
            });

            if (!string.IsNullOrEmpty(_options.ConfigurationFilePath))
            {
                await SaveAsync();
            }
        }

        return result;
    }

    public Task<YarpConfiguration> GetConfigurationAsync()
    {
        return Task.FromResult(new YarpConfiguration
        {
            Routes = _routes.Values.ToList(),
            Clusters = _clusters.Values.ToList()
        });
    }

    public async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(_options.ConfigurationFilePath))
            return;

        await _fileLock.WaitAsync();
        try
        {
            var config = await GetConfigurationAsync();
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var directory = Path.GetDirectoryName(_options.ConfigurationFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(_options.ConfigurationFilePath, json);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task LoadAsync()
    {
        if (string.IsNullOrEmpty(_options.ConfigurationFilePath) || 
            !File.Exists(_options.ConfigurationFilePath))
            return;

        await _fileLock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(_options.ConfigurationFilePath);
            var config = JsonSerializer.Deserialize<YarpConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (config != null)
            {
                _routes.Clear();
                _clusters.Clear();

                foreach (var route in config.Routes)
                {
                    _routes[route.RouteId] = route;
                }

                foreach (var cluster in config.Clusters)
                {
                    _clusters[cluster.ClusterId] = cluster;
                }

                OnConfigurationChanged(new ConfigurationChangedEventArgs
                {
                    ChangeType = ChangeType.Reloaded
                });
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    protected virtual void OnConfigurationChanged(ConfigurationChangedEventArgs e)
    {
        ConfigurationChanged?.Invoke(this, e);
    }
}
