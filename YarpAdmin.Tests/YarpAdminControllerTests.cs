using Microsoft.AspNetCore.Mvc;
using Moq;
using YarpAdmin;
using YarpAdmin.Controllers;
using YarpAdmin.Models;

namespace YarpAdmin.Tests;

public class YarpAdminControllerTests
{
    private readonly Mock<IYarpAdminService> _mockService;
    private readonly YarpAdminOptions _options;
    private readonly YarpAdminController _controller;

    public YarpAdminControllerTests()
    {
        _mockService = new Mock<IYarpAdminService>();
        _options = new YarpAdminOptions { AllowConfigurationChanges = true };
        _controller = new YarpAdminController(_mockService.Object, _options);
    }

    #region Route Tests - GET

    [Fact]
    public async Task GetRoutes_ReturnsOkWithRoutes()
    {
        var routes = new List<RouteConfig>
        {
            new RouteConfig { RouteId = "route-1", ClusterId = "cluster-1" },
            new RouteConfig { RouteId = "route-2", ClusterId = "cluster-2" }
        };
        _mockService.Setup(s => s.GetRoutesAsync()).ReturnsAsync(routes);

        var result = await _controller.GetRoutes();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRoutes = Assert.IsAssignableFrom<IEnumerable<RouteConfig>>(okResult.Value);
        Assert.Equal(2, returnedRoutes.Count());
    }

    [Fact]
    public async Task GetRoute_ExistingRoute_ReturnsOk()
    {
        var route = new RouteConfig { RouteId = "test-route", ClusterId = "test-cluster" };
        _mockService.Setup(s => s.GetRouteAsync("test-route")).ReturnsAsync(route);

        var result = await _controller.GetRoute("test-route");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRoute = Assert.IsType<RouteConfig>(okResult.Value);
        Assert.Equal("test-route", returnedRoute.RouteId);
    }

    [Fact]
    public async Task GetRoute_NonExistingRoute_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetRouteAsync("non-existing")).ReturnsAsync((RouteConfig?)null);

        var result = await _controller.GetRoute("non-existing");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Route Tests - POST

    [Fact]
    public async Task CreateRoute_ValidRoute_ReturnsCreated()
    {
        var route = new RouteConfig { RouteId = "new-route", ClusterId = "test-cluster" };
        _mockService.Setup(s => s.GetRouteAsync("new-route")).ReturnsAsync((RouteConfig?)null);
        _mockService.Setup(s => s.UpsertRouteAsync(route)).ReturnsAsync(route);

        var result = await _controller.CreateRoute(route);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetRoute", createdResult.ActionName);
        Assert.Equal("new-route", ((RouteConfig)createdResult.Value!).RouteId);
    }

    [Fact]
    public async Task CreateRoute_EmptyRouteId_ReturnsBadRequest()
    {
        var route = new RouteConfig { RouteId = "", ClusterId = "test-cluster" };

        var result = await _controller.CreateRoute(route);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateRoute_WhitespaceRouteId_ReturnsBadRequest()
    {
        var route = new RouteConfig { RouteId = "   ", ClusterId = "test-cluster" };

        var result = await _controller.CreateRoute(route);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateRoute_ExistingRoute_ReturnsConflict()
    {
        var existingRoute = new RouteConfig { RouteId = "existing-route", ClusterId = "test-cluster" };
        _mockService.Setup(s => s.GetRouteAsync("existing-route")).ReturnsAsync(existingRoute);

        var route = new RouteConfig { RouteId = "existing-route", ClusterId = "new-cluster" };
        var result = await _controller.CreateRoute(route);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateRoute_ConfigurationChangesDisabled_ReturnsForbidden()
    {
        var options = new YarpAdminOptions { AllowConfigurationChanges = false };
        var controller = new YarpAdminController(_mockService.Object, options);
        var route = new RouteConfig { RouteId = "new-route", ClusterId = "test-cluster" };

        var result = await controller.CreateRoute(route);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    #endregion

    #region Route Tests - PUT

    [Fact]
    public async Task UpdateRoute_ValidRoute_ReturnsOk()
    {
        var route = new RouteConfig { RouteId = "test-route", ClusterId = "updated-cluster" };
        _mockService.Setup(s => s.GetRouteAsync("test-route")).ReturnsAsync(route);
        _mockService.Setup(s => s.UpsertRouteAsync(route)).ReturnsAsync(route);

        var result = await _controller.UpdateRoute("test-route", route);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("updated-cluster", ((RouteConfig)okResult.Value!).ClusterId);
    }

    [Fact]
    public async Task UpdateRoute_MismatchedRouteId_ReturnsBadRequest()
    {
        var route = new RouteConfig { RouteId = "different-route", ClusterId = "test-cluster" };

        var result = await _controller.UpdateRoute("test-route", route);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateRoute_NonExistingRoute_ReturnsNotFound()
    {
        var route = new RouteConfig { RouteId = "non-existing", ClusterId = "test-cluster" };
        _mockService.Setup(s => s.GetRouteAsync("non-existing")).ReturnsAsync((RouteConfig?)null);

        var result = await _controller.UpdateRoute("non-existing", route);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateRoute_ConfigurationChangesDisabled_ReturnsForbidden()
    {
        var options = new YarpAdminOptions { AllowConfigurationChanges = false };
        var controller = new YarpAdminController(_mockService.Object, options);
        var route = new RouteConfig { RouteId = "test-route", ClusterId = "test-cluster" };

        var result = await controller.UpdateRoute("test-route", route);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    #endregion

    #region Route Tests - DELETE

    [Fact]
    public async Task DeleteRoute_ExistingRoute_ReturnsNoContent()
    {
        _mockService.Setup(s => s.DeleteRouteAsync("test-route")).ReturnsAsync(true);

        var result = await _controller.DeleteRoute("test-route");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteRoute_NonExistingRoute_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteRouteAsync("non-existing")).ReturnsAsync(false);

        var result = await _controller.DeleteRoute("non-existing");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteRoute_ConfigurationChangesDisabled_ReturnsForbidden()
    {
        var options = new YarpAdminOptions { AllowConfigurationChanges = false };
        var controller = new YarpAdminController(_mockService.Object, options);

        var result = await controller.DeleteRoute("test-route");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    #endregion

    #region Cluster Tests - GET

    [Fact]
    public async Task GetClusters_ReturnsOkWithClusters()
    {
        var clusters = new List<ClusterConfig>
        {
            new ClusterConfig { ClusterId = "cluster-1" },
            new ClusterConfig { ClusterId = "cluster-2" }
        };
        _mockService.Setup(s => s.GetClustersAsync()).ReturnsAsync(clusters);

        var result = await _controller.GetClusters();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedClusters = Assert.IsAssignableFrom<IEnumerable<ClusterConfig>>(okResult.Value);
        Assert.Equal(2, returnedClusters.Count());
    }

    [Fact]
    public async Task GetCluster_ExistingCluster_ReturnsOk()
    {
        var cluster = new ClusterConfig { ClusterId = "test-cluster" };
        _mockService.Setup(s => s.GetClusterAsync("test-cluster")).ReturnsAsync(cluster);

        var result = await _controller.GetCluster("test-cluster");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCluster = Assert.IsType<ClusterConfig>(okResult.Value);
        Assert.Equal("test-cluster", returnedCluster.ClusterId);
    }

    [Fact]
    public async Task GetCluster_NonExistingCluster_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetClusterAsync("non-existing")).ReturnsAsync((ClusterConfig?)null);

        var result = await _controller.GetCluster("non-existing");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Cluster Tests - POST

    [Fact]
    public async Task CreateCluster_ValidCluster_ReturnsCreated()
    {
        var cluster = new ClusterConfig { ClusterId = "new-cluster" };
        _mockService.Setup(s => s.GetClusterAsync("new-cluster")).ReturnsAsync((ClusterConfig?)null);
        _mockService.Setup(s => s.UpsertClusterAsync(cluster)).ReturnsAsync(cluster);

        var result = await _controller.CreateCluster(cluster);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetCluster", createdResult.ActionName);
        Assert.Equal("new-cluster", ((ClusterConfig)createdResult.Value!).ClusterId);
    }

    [Fact]
    public async Task CreateCluster_EmptyClusterId_ReturnsBadRequest()
    {
        var cluster = new ClusterConfig { ClusterId = "" };

        var result = await _controller.CreateCluster(cluster);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateCluster_ExistingCluster_ReturnsConflict()
    {
        var existingCluster = new ClusterConfig { ClusterId = "existing-cluster" };
        _mockService.Setup(s => s.GetClusterAsync("existing-cluster")).ReturnsAsync(existingCluster);

        var cluster = new ClusterConfig { ClusterId = "existing-cluster" };
        var result = await _controller.CreateCluster(cluster);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateCluster_ConfigurationChangesDisabled_ReturnsForbidden()
    {
        var options = new YarpAdminOptions { AllowConfigurationChanges = false };
        var controller = new YarpAdminController(_mockService.Object, options);
        var cluster = new ClusterConfig { ClusterId = "new-cluster" };

        var result = await controller.CreateCluster(cluster);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    #endregion

    #region Cluster Tests - PUT

    [Fact]
    public async Task UpdateCluster_ValidCluster_ReturnsOk()
    {
        var cluster = new ClusterConfig { ClusterId = "test-cluster", LoadBalancingPolicy = "Random" };
        _mockService.Setup(s => s.GetClusterAsync("test-cluster")).ReturnsAsync(cluster);
        _mockService.Setup(s => s.UpsertClusterAsync(cluster)).ReturnsAsync(cluster);

        var result = await _controller.UpdateCluster("test-cluster", cluster);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Random", ((ClusterConfig)okResult.Value!).LoadBalancingPolicy);
    }

    [Fact]
    public async Task UpdateCluster_MismatchedClusterId_ReturnsBadRequest()
    {
        var cluster = new ClusterConfig { ClusterId = "different-cluster" };

        var result = await _controller.UpdateCluster("test-cluster", cluster);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateCluster_NonExistingCluster_ReturnsNotFound()
    {
        var cluster = new ClusterConfig { ClusterId = "non-existing" };
        _mockService.Setup(s => s.GetClusterAsync("non-existing")).ReturnsAsync((ClusterConfig?)null);

        var result = await _controller.UpdateCluster("non-existing", cluster);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateCluster_ConfigurationChangesDisabled_ReturnsForbidden()
    {
        var options = new YarpAdminOptions { AllowConfigurationChanges = false };
        var controller = new YarpAdminController(_mockService.Object, options);
        var cluster = new ClusterConfig { ClusterId = "test-cluster" };

        var result = await controller.UpdateCluster("test-cluster", cluster);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    #endregion

    #region Cluster Tests - DELETE

    [Fact]
    public async Task DeleteCluster_ExistingCluster_ReturnsNoContent()
    {
        _mockService.Setup(s => s.DeleteClusterAsync("test-cluster")).ReturnsAsync(true);

        var result = await _controller.DeleteCluster("test-cluster");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteCluster_NonExistingCluster_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteClusterAsync("non-existing")).ReturnsAsync(false);

        var result = await _controller.DeleteCluster("non-existing");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteCluster_ConfigurationChangesDisabled_ReturnsForbidden()
    {
        var options = new YarpAdminOptions { AllowConfigurationChanges = false };
        var controller = new YarpAdminController(_mockService.Object, options);

        var result = await controller.DeleteCluster("test-cluster");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task ApplyConfiguration_ReturnsOk()
    {
        _mockService.Setup(s => s.ApplyConfigurationAsync()).Returns(Task.CompletedTask);

        var result = await _controller.ApplyConfiguration();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ApplyConfiguration_ConfigurationChangesDisabled_ReturnsForbidden()
    {
        var options = new YarpAdminOptions { AllowConfigurationChanges = false };
        var controller = new YarpAdminController(_mockService.Object, options);

        var result = await controller.ApplyConfiguration();

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetConfiguration_ReturnsCompleteConfiguration()
    {
        var routes = new List<RouteConfig>
        {
            new RouteConfig { RouteId = "route-1", ClusterId = "cluster-1" }
        };
        var clusters = new List<ClusterConfig>
        {
            new ClusterConfig { ClusterId = "cluster-1" }
        };
        _mockService.Setup(s => s.GetRoutesAsync()).ReturnsAsync(routes);
        _mockService.Setup(s => s.GetClustersAsync()).ReturnsAsync(clusters);

        var result = await _controller.GetConfiguration();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var config = Assert.IsType<YarpConfiguration>(okResult.Value);
        Assert.Single(config.Routes);
        Assert.Single(config.Clusters);
    }

    [Fact]
    public async Task ImportConfiguration_ValidConfig_ReturnsOk()
    {
        var config = new YarpConfiguration
        {
            Routes = new List<RouteConfig>
            {
                new RouteConfig { RouteId = "route-1", ClusterId = "cluster-1" }
            },
            Clusters = new List<ClusterConfig>
            {
                new ClusterConfig { ClusterId = "cluster-1" }
            }
        };
        _mockService.Setup(s => s.UpsertClusterAsync(It.IsAny<ClusterConfig>()))
            .ReturnsAsync((ClusterConfig c) => c);
        _mockService.Setup(s => s.UpsertRouteAsync(It.IsAny<RouteConfig>()))
            .ReturnsAsync((RouteConfig r) => r);
        _mockService.Setup(s => s.ApplyConfigurationAsync()).Returns(Task.CompletedTask);

        var result = await _controller.ImportConfiguration(config);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.UpsertClusterAsync(It.IsAny<ClusterConfig>()), Times.Once);
        _mockService.Verify(s => s.UpsertRouteAsync(It.IsAny<RouteConfig>()), Times.Once);
        _mockService.Verify(s => s.ApplyConfigurationAsync(), Times.Once);
    }

    [Fact]
    public async Task ImportConfiguration_ConfigurationChangesDisabled_ReturnsForbidden()
    {
        var options = new YarpAdminOptions { AllowConfigurationChanges = false };
        var controller = new YarpAdminController(_mockService.Object, options);
        var config = new YarpConfiguration();

        var result = await controller.ImportConfiguration(config);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    #endregion
}
