# YARP Admin

A comprehensive administration UI and REST API for [YARP (Yet Another Reverse Proxy)](https://microsoft.github.io/reverse-proxy/). Provides a modern React-based dashboard for managing routes, clusters, and destinations with real-time configuration updates.

[![CI](https://github.com/ranjeethmd/yarp-admin/actions/workflows/ci.yml/badge.svg)](https://github.com/ranjeethmd/yarp-admin/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/YarpAdmin.svg)](https://www.nuget.org/packages/YarpAdmin/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/YarpAdmin.svg)](https://www.nuget.org/packages/YarpAdmin/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Features

- **Modern Admin UI** - React-based dashboard with intuitive toggle-to-enable interface
- **Full YARP Feature Support** - Configure all YARP features through the UI
- **REST API** - Complete API for programmatic configuration management
- **Real-time Updates** - Apply configuration changes without restarting
- **Custom Configuration Store** - Extensible storage with in-memory default
- **Authentication Support** - Optional authentication and authorization
- **Request Logging** - Built-in middleware for request/response logging

## Supported YARP Features

### Route Configuration
- Path matching with wildcards
- HTTP method filtering
- Host-based routing
- Header matching (exact, prefix, contains, regex)
- Query parameter matching
- Authorization policies
- CORS policies
- Rate limiter policies
- Timeout policies
- Request/response transforms
- Custom metadata

### Cluster Configuration
- Multiple load balancing policies (RoundRobin, Random, PowerOfTwoChoices, LeastRequests, FirstAlphabetical)
- Session affinity (Cookie, CustomHeader)
- Health checks (Active and Passive)
- HTTP client settings (SSL, connection limits, HTTP/2)
- HTTP request settings (timeouts, version policy)
- Multiple destinations with health endpoints
- Custom metadata

## Installation

```bash
dotnet add package YarpAdmin
```

Or via Package Manager:

```powershell
Install-Package YarpAdmin
```

## Quick Start

### 1. Add Services

```csharp
using YarpAdmin;

var builder = WebApplication.CreateBuilder(args);

// Add YARP Admin with default in-memory store
builder.Services.AddYarpAdmin(options =>
{
    options.Title = "My YARP Admin";
    options.AllowConfigurationChanges = true;
});

// Add YARP reverse proxy
builder.Services.AddReverseProxy();

var app = builder.Build();
```

### 2. Configure Middleware

```csharp
using YarpAdmin.Middleware;

// Optional: Add authentication and logging middleware
app.UseYarpAdminAuth();
app.UseYarpAdminLogging();

// Map YARP Admin endpoints and UI
app.MapYarpAdmin("/yarp-admin");

// Map YARP reverse proxy
app.MapReverseProxy();

app.Run();
```

### 3. Access the Dashboard

Navigate to `https://localhost:5001/yarp-admin` to access the admin UI.

## Configuration Options

```csharp
builder.Services.AddYarpAdmin(options =>
{
    // UI title
    options.Title = "YARP Admin";

    // Enable/disable configuration changes (read-only mode)
    options.AllowConfigurationChanges = true;

    // Require authentication
    options.RequireAuthentication = false;
    options.AuthenticationPolicy = "YarpAdminPolicy";

    // Persist configuration to file
    options.ConfigurationFilePath = "yarp-config.json";
});
```

## Authentication Setup

```csharp
// Add authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://your-auth-server";
        options.Audience = "yarp-admin";
    });

// Add authorization policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("YarpAdminPolicy", policy =>
        policy.RequireRole("Admin"));
});

// Configure YARP Admin with authentication
builder.Services.AddYarpAdmin(options =>
{
    options.RequireAuthentication = true;
    options.AuthenticationPolicy = "YarpAdminPolicy";
});
```

## Custom Configuration Store

Implement `IYarpConfigurationStore` for custom persistence:

```csharp
public class SqlYarpConfigurationStore : IYarpConfigurationStore
{
    public Task<IEnumerable<RouteConfig>> GetRoutesAsync() { /* ... */ }
    public Task<IEnumerable<ClusterConfig>> GetClustersAsync() { /* ... */ }
    public Task SaveRouteAsync(RouteConfig route) { /* ... */ }
    public Task SaveClusterAsync(ClusterConfig cluster) { /* ... */ }
    public Task DeleteRouteAsync(string routeId) { /* ... */ }
    public Task DeleteClusterAsync(string clusterId) { /* ... */ }
}

// Register custom store
builder.Services.AddYarpAdmin<SqlYarpConfigurationStore>(options =>
{
    options.Title = "My YARP Admin";
});
```

## REST API Reference

### Routes

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/yarp-admin/routes` | Get all routes |
| GET | `/api/yarp-admin/routes/{routeId}` | Get route by ID |
| POST | `/api/yarp-admin/routes` | Create new route |
| PUT | `/api/yarp-admin/routes/{routeId}` | Update route |
| DELETE | `/api/yarp-admin/routes/{routeId}` | Delete route |

### Clusters

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/yarp-admin/clusters` | Get all clusters |
| GET | `/api/yarp-admin/clusters/{clusterId}` | Get cluster by ID |
| POST | `/api/yarp-admin/clusters` | Create new cluster |
| PUT | `/api/yarp-admin/clusters/{clusterId}` | Update cluster |
| DELETE | `/api/yarp-admin/clusters/{clusterId}` | Delete cluster |

### Configuration

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/yarp-admin/config` | Get complete configuration |
| POST | `/api/yarp-admin/config/import` | Import configuration |
| POST | `/api/yarp-admin/apply` | Apply configuration to YARP |

## API Examples

### Create a Cluster

```bash
curl -X POST http://localhost:5000/api/yarp-admin/clusters \
  -H "Content-Type: application/json" \
  -d '{
    "clusterId": "api-cluster",
    "loadBalancingPolicy": "RoundRobin",
    "destinations": {
      "api-1": { "address": "https://api1.example.com" },
      "api-2": { "address": "https://api2.example.com" }
    }
  }'
```

### Create a Route

```bash
curl -X POST http://localhost:5000/api/yarp-admin/routes \
  -H "Content-Type: application/json" \
  -d '{
    "routeId": "api-route",
    "clusterId": "api-cluster",
    "match": {
      "path": "/api/{**catch-all}",
      "methods": ["GET", "POST", "PUT", "DELETE"]
    }
  }'
```

### Configure Health Checks

```bash
curl -X POST http://localhost:5000/api/yarp-admin/clusters \
  -H "Content-Type: application/json" \
  -d '{
    "clusterId": "api-cluster",
    "loadBalancingPolicy": "RoundRobin",
    "healthCheck": {
      "active": {
        "enabled": true,
        "interval": "00:00:15",
        "timeout": "00:00:10",
        "policy": "ConsecutiveFailures",
        "path": "/health"
      },
      "passive": {
        "enabled": true,
        "policy": "TransportFailureRate",
        "reactivationPeriod": "00:02:00"
      }
    },
    "destinations": {
      "api-1": {
        "address": "https://api1.example.com",
        "health": "https://api1.example.com/health"
      }
    }
  }'
```

### Configure Session Affinity

```bash
curl -X POST http://localhost:5000/api/yarp-admin/clusters \
  -H "Content-Type: application/json" \
  -d '{
    "clusterId": "web-cluster",
    "loadBalancingPolicy": "RoundRobin",
    "sessionAffinity": {
      "enabled": true,
      "policy": "Cookie",
      "failurePolicy": "Redistribute",
      "affinityKeyName": ".MyApp.Affinity"
    },
    "destinations": {
      "web-1": { "address": "https://web1.example.com" }
    }
  }'
```

### Configure Route with Transforms

```bash
curl -X POST http://localhost:5000/api/yarp-admin/routes \
  -H "Content-Type: application/json" \
  -d '{
    "routeId": "api-route",
    "clusterId": "api-cluster",
    "match": { "path": "/api/{**catch-all}" },
    "transforms": [
      { "PathRemovePrefix": "/api" },
      { "RequestHeader": "X-Forwarded-Host", "Set": "{host}" }
    ]
  }'
```

### Apply Configuration

```bash
curl -X POST http://localhost:5000/api/yarp-admin/apply
```

## UI Features

The admin dashboard provides a toggle-to-enable pattern where advanced features are collapsed by default and expand when enabled.

### Route Modal Features

- **Basic Matching** - Path pattern and HTTP methods
- **Advanced Matching** - Hosts, headers, query parameters
- **Policies** - Authorization, CORS, rate limiter, timeout
- **Transforms** - Request/response modifications
- **Metadata** - Custom key-value pairs

### Cluster Modal Features

- **Load Balancing** - Policy selection
- **Destinations** - Multiple backend servers with health endpoints
- **Session Affinity** - Cookie or header-based sticky sessions
- **Health Checks** - Active probing and passive monitoring
- **HTTP Client** - SSL protocols, connection limits, HTTP/2
- **HTTP Request** - Timeouts, version policy
- **Metadata** - Custom key-value pairs

## Project Structure

```
yarp-admin/
├── YarpAdmin/                    # Main library
│   ├── Controllers/              # API controllers
│   ├── Middleware/               # Auth and logging middleware
│   ├── Models/                   # Configuration models
│   ├── Services/                 # Admin service and config store
│   ├── wwwroot/                  # React UI (embedded)
│   └── YarpAdminExtensions.cs    # DI extensions
├── YarpAdmin.Tests/              # Unit and integration tests
│   ├── YarpAdminControllerTests.cs
│   ├── YarpAdminServiceTests.cs
│   ├── InMemoryYarpConfigurationStoreTests.cs
│   ├── YarpAdminExtensionsTests.cs
│   ├── MiddlewareTests.cs
│   └── ModelTests.cs
├── Example/                      # Example application
│   └── Program.cs                # Usage example
└── README.md
```

## Supported .NET Versions

- .NET 9.0
- .NET 10.0

## Building from Source

```bash
# Clone the repository
git clone https://github.com/ranjeethmd/yarp-admin.git
cd yarp-admin

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the example
dotnet run --project Example
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests with detailed output
dotnet test --verbosity normal
```

The test suite includes 126 tests covering:
- **Controller Tests** - API endpoint behavior, validation, and error handling
- **Service Tests** - Business logic and YARP configuration conversion
- **Configuration Store Tests** - CRUD operations, persistence, and events
- **Middleware Tests** - Authentication and logging middleware
- **Model Tests** - Serialization, default values, and data integrity
- **Extension Tests** - DI registration and endpoint mapping

## CI/CD

This project uses GitHub Actions for continuous integration and deployment:

- **CI Workflow** (`ci.yml`) - Runs on every push and pull request to `main`/`master`. Builds the solution and runs all tests.
- **Publish Workflow** (`publish.yml`) - Manual trigger to publish packages to NuGet.org. Go to Actions → "Publish to NuGet" → "Run workflow".

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [YARP](https://microsoft.github.io/reverse-proxy/) - Microsoft's reverse proxy toolkit
- [React](https://reactjs.org/) - UI library
