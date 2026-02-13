using System.Text.Json.Serialization;

namespace YarpAdmin.Models;

/// <summary>
/// Represents a YARP route configuration.
/// </summary>
public class RouteConfig
{
    /// <summary>
    /// Unique identifier for the route.
    /// </summary>
    [JsonPropertyName("routeId")]
    public string RouteId { get; set; } = string.Empty;

    /// <summary>
    /// The cluster this route forwards to.
    /// </summary>
    [JsonPropertyName("clusterId")]
    public string ClusterId { get; set; } = string.Empty;

    /// <summary>
    /// Route matching criteria.
    /// </summary>
    [JsonPropertyName("match")]
    public RouteMatch? Match { get; set; }

    /// <summary>
    /// Route order (lower values are higher priority).
    /// </summary>
    [JsonPropertyName("order")]
    public int? Order { get; set; }

    /// <summary>
    /// Whether this route is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Authorization policy for this route.
    /// </summary>
    [JsonPropertyName("authorizationPolicy")]
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// CORS policy for this route.
    /// </summary>
    [JsonPropertyName("corsPolicy")]
    public string? CorsPolicy { get; set; }

    /// <summary>
    /// Rate limiter policy for this route.
    /// </summary>
    [JsonPropertyName("rateLimiterPolicy")]
    public string? RateLimiterPolicy { get; set; }

    /// <summary>
    /// Timeout policy for this route.
    /// </summary>
    [JsonPropertyName("timeoutPolicy")]
    public string? TimeoutPolicy { get; set; }

    /// <summary>
    /// Request transforms for this route.
    /// </summary>
    [JsonPropertyName("transforms")]
    public List<Dictionary<string, string>>? Transforms { get; set; }

    /// <summary>
    /// Additional metadata for this route.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Represents route matching criteria.
/// </summary>
public class RouteMatch
{
    /// <summary>
    /// The path pattern to match.
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// HTTP methods to match (null means all methods).
    /// </summary>
    [JsonPropertyName("methods")]
    public List<string>? Methods { get; set; }

    /// <summary>
    /// Hosts to match.
    /// </summary>
    [JsonPropertyName("hosts")]
    public List<string>? Hosts { get; set; }

    /// <summary>
    /// Headers to match.
    /// </summary>
    [JsonPropertyName("headers")]
    public List<RouteHeader>? Headers { get; set; }

    /// <summary>
    /// Query parameters to match.
    /// </summary>
    [JsonPropertyName("queryParameters")]
    public List<RouteQueryParameter>? QueryParameters { get; set; }
}

/// <summary>
/// Represents a header matching rule.
/// </summary>
public class RouteHeader
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("values")]
    public List<string>? Values { get; set; }

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "ExactHeader";

    [JsonPropertyName("isCaseSensitive")]
    public bool IsCaseSensitive { get; set; } = false;
}

/// <summary>
/// Represents a query parameter matching rule.
/// </summary>
public class RouteQueryParameter
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("values")]
    public List<string>? Values { get; set; }

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "Exact";

    [JsonPropertyName("isCaseSensitive")]
    public bool IsCaseSensitive { get; set; } = false;
}

/// <summary>
/// Represents a YARP cluster configuration.
/// </summary>
public class ClusterConfig
{
    /// <summary>
    /// Unique identifier for the cluster.
    /// </summary>
    [JsonPropertyName("clusterId")]
    public string ClusterId { get; set; } = string.Empty;

    /// <summary>
    /// Load balancing policy for this cluster.
    /// </summary>
    [JsonPropertyName("loadBalancingPolicy")]
    public string? LoadBalancingPolicy { get; set; }

    /// <summary>
    /// Session affinity configuration.
    /// </summary>
    [JsonPropertyName("sessionAffinity")]
    public SessionAffinityConfig? SessionAffinity { get; set; }

    /// <summary>
    /// Health check configuration.
    /// </summary>
    [JsonPropertyName("healthCheck")]
    public HealthCheckConfig? HealthCheck { get; set; }

    /// <summary>
    /// HTTP client configuration.
    /// </summary>
    [JsonPropertyName("httpClient")]
    public HttpClientConfig? HttpClient { get; set; }

    /// <summary>
    /// HTTP request configuration.
    /// </summary>
    [JsonPropertyName("httpRequest")]
    public HttpRequestConfig? HttpRequest { get; set; }

    /// <summary>
    /// Destinations in this cluster.
    /// </summary>
    [JsonPropertyName("destinations")]
    public Dictionary<string, DestinationConfig>? Destinations { get; set; }

    /// <summary>
    /// Additional metadata for this cluster.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Represents a destination (backend server) configuration.
/// </summary>
public class DestinationConfig
{
    /// <summary>
    /// The address of the destination.
    /// </summary>
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Optional health check address.
    /// </summary>
    [JsonPropertyName("health")]
    public string? Health { get; set; }

    /// <summary>
    /// Additional metadata for this destination.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Represents session affinity configuration.
/// </summary>
public class SessionAffinityConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("policy")]
    public string? Policy { get; set; }

    [JsonPropertyName("failurePolicy")]
    public string? FailurePolicy { get; set; }

    [JsonPropertyName("affinityKeyName")]
    public string? AffinityKeyName { get; set; }
}

/// <summary>
/// Represents health check configuration.
/// </summary>
public class HealthCheckConfig
{
    [JsonPropertyName("passive")]
    public PassiveHealthCheckConfig? Passive { get; set; }

    [JsonPropertyName("active")]
    public ActiveHealthCheckConfig? Active { get; set; }

    [JsonPropertyName("availableDestinationsPolicy")]
    public string? AvailableDestinationsPolicy { get; set; }
}

public class PassiveHealthCheckConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("policy")]
    public string? Policy { get; set; }

    [JsonPropertyName("reactivationPeriod")]
    public string? ReactivationPeriod { get; set; }
}

public class ActiveHealthCheckConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("interval")]
    public string? Interval { get; set; }

    [JsonPropertyName("timeout")]
    public string? Timeout { get; set; }

    [JsonPropertyName("policy")]
    public string? Policy { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }
}

/// <summary>
/// Represents HTTP client configuration.
/// </summary>
public class HttpClientConfig
{
    [JsonPropertyName("sslProtocols")]
    public List<string>? SslProtocols { get; set; }

    [JsonPropertyName("dangerousAcceptAnyServerCertificate")]
    public bool? DangerousAcceptAnyServerCertificate { get; set; }

    [JsonPropertyName("maxConnectionsPerServer")]
    public int? MaxConnectionsPerServer { get; set; }

    [JsonPropertyName("enableMultipleHttp2Connections")]
    public bool? EnableMultipleHttp2Connections { get; set; }

    [JsonPropertyName("requestHeaderEncoding")]
    public string? RequestHeaderEncoding { get; set; }

    [JsonPropertyName("responseHeaderEncoding")]
    public string? ResponseHeaderEncoding { get; set; }
}

/// <summary>
/// Represents HTTP request configuration.
/// </summary>
public class HttpRequestConfig
{
    [JsonPropertyName("activityTimeout")]
    public string? ActivityTimeout { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("versionPolicy")]
    public string? VersionPolicy { get; set; }

    [JsonPropertyName("allowResponseBuffering")]
    public bool? AllowResponseBuffering { get; set; }
}

/// <summary>
/// Represents the complete YARP configuration.
/// </summary>
public class YarpConfiguration
{
    [JsonPropertyName("routes")]
    public List<RouteConfig> Routes { get; set; } = new();

    [JsonPropertyName("clusters")]
    public List<ClusterConfig> Clusters { get; set; } = new();
}
