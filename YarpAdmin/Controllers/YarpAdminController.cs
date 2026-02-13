using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YarpAdmin.Models;

namespace YarpAdmin.Controllers;

/// <summary>
/// API controller for YARP administration.
/// </summary>
[ApiController]
[Route("api/yarp-admin")]
public class YarpAdminController : ControllerBase
{
    private readonly IYarpAdminService _adminService;
    private readonly YarpAdminOptions _options;

    public YarpAdminController(IYarpAdminService adminService, YarpAdminOptions options)
    {
        _adminService = adminService;
        _options = options;
    }

    #region Routes

    /// <summary>
    /// Gets all configured routes.
    /// </summary>
    [HttpGet("routes")]
    public async Task<ActionResult<IEnumerable<RouteConfig>>> GetRoutes()
    {
        var routes = await _adminService.GetRoutesAsync();
        return Ok(routes);
    }

    /// <summary>
    /// Gets a specific route by ID.
    /// </summary>
    [HttpGet("routes/{routeId}")]
    public async Task<ActionResult<RouteConfig>> GetRoute(string routeId)
    {
        var route = await _adminService.GetRouteAsync(routeId);
        if (route == null)
            return NotFound(new { message = $"Route '{routeId}' not found" });
        return Ok(route);
    }

    /// <summary>
    /// Creates a new route.
    /// </summary>
    [HttpPost("routes")]
    public async Task<ActionResult<RouteConfig>> CreateRoute([FromBody] RouteConfig route)
    {
        if (!_options.AllowConfigurationChanges)
            return StatusCode(403, new { message = "Configuration changes are not allowed" });

        if (string.IsNullOrWhiteSpace(route.RouteId))
            return BadRequest(new { message = "RouteId is required" });

        var existing = await _adminService.GetRouteAsync(route.RouteId);
        if (existing != null)
            return Conflict(new { message = $"Route '{route.RouteId}' already exists" });

        var result = await _adminService.UpsertRouteAsync(route);
        return CreatedAtAction(nameof(GetRoute), new { routeId = result.RouteId }, result);
    }

    /// <summary>
    /// Updates an existing route.
    /// </summary>
    [HttpPut("routes/{routeId}")]
    public async Task<ActionResult<RouteConfig>> UpdateRoute(string routeId, [FromBody] RouteConfig route)
    {
        if (!_options.AllowConfigurationChanges)
            return StatusCode(403, new { message = "Configuration changes are not allowed" });

        if (route.RouteId != routeId)
            return BadRequest(new { message = "RouteId in URL and body must match" });

        var existing = await _adminService.GetRouteAsync(routeId);
        if (existing == null)
            return NotFound(new { message = $"Route '{routeId}' not found" });

        var result = await _adminService.UpsertRouteAsync(route);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a route.
    /// </summary>
    [HttpDelete("routes/{routeId}")]
    public async Task<ActionResult> DeleteRoute(string routeId)
    {
        if (!_options.AllowConfigurationChanges)
            return StatusCode(403, new { message = "Configuration changes are not allowed" });

        var success = await _adminService.DeleteRouteAsync(routeId);
        if (!success)
            return NotFound(new { message = $"Route '{routeId}' not found" });
        return NoContent();
    }

    #endregion

    #region Clusters

    /// <summary>
    /// Gets all configured clusters.
    /// </summary>
    [HttpGet("clusters")]
    public async Task<ActionResult<IEnumerable<ClusterConfig>>> GetClusters()
    {
        var clusters = await _adminService.GetClustersAsync();
        return Ok(clusters);
    }

    /// <summary>
    /// Gets a specific cluster by ID.
    /// </summary>
    [HttpGet("clusters/{clusterId}")]
    public async Task<ActionResult<ClusterConfig>> GetCluster(string clusterId)
    {
        var cluster = await _adminService.GetClusterAsync(clusterId);
        if (cluster == null)
            return NotFound(new { message = $"Cluster '{clusterId}' not found" });
        return Ok(cluster);
    }

    /// <summary>
    /// Creates a new cluster.
    /// </summary>
    [HttpPost("clusters")]
    public async Task<ActionResult<ClusterConfig>> CreateCluster([FromBody] ClusterConfig cluster)
    {
        if (!_options.AllowConfigurationChanges)
            return StatusCode(403, new { message = "Configuration changes are not allowed" });

        if (string.IsNullOrWhiteSpace(cluster.ClusterId))
            return BadRequest(new { message = "ClusterId is required" });

        var existing = await _adminService.GetClusterAsync(cluster.ClusterId);
        if (existing != null)
            return Conflict(new { message = $"Cluster '{cluster.ClusterId}' already exists" });

        var result = await _adminService.UpsertClusterAsync(cluster);
        return CreatedAtAction(nameof(GetCluster), new { clusterId = result.ClusterId }, result);
    }

    /// <summary>
    /// Updates an existing cluster.
    /// </summary>
    [HttpPut("clusters/{clusterId}")]
    public async Task<ActionResult<ClusterConfig>> UpdateCluster(string clusterId, [FromBody] ClusterConfig cluster)
    {
        if (!_options.AllowConfigurationChanges)
            return StatusCode(403, new { message = "Configuration changes are not allowed" });

        if (cluster.ClusterId != clusterId)
            return BadRequest(new { message = "ClusterId in URL and body must match" });

        var existing = await _adminService.GetClusterAsync(clusterId);
        if (existing == null)
            return NotFound(new { message = $"Cluster '{clusterId}' not found" });

        var result = await _adminService.UpsertClusterAsync(cluster);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a cluster.
    /// </summary>
    [HttpDelete("clusters/{clusterId}")]
    public async Task<ActionResult> DeleteCluster(string clusterId)
    {
        if (!_options.AllowConfigurationChanges)
            return StatusCode(403, new { message = "Configuration changes are not allowed" });

        var success = await _adminService.DeleteClusterAsync(clusterId);
        if (!success)
            return NotFound(new { message = $"Cluster '{clusterId}' not found" });
        return NoContent();
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Applies the current configuration to YARP.
    /// </summary>
    [HttpPost("apply")]
    public async Task<ActionResult> ApplyConfiguration()
    {
        if (!_options.AllowConfigurationChanges)
            return StatusCode(403, new { message = "Configuration changes are not allowed" });

        await _adminService.ApplyConfigurationAsync();
        return Ok(new { message = "Configuration applied successfully" });
    }

    /// <summary>
    /// Gets the complete YARP configuration.
    /// </summary>
    [HttpGet("config")]
    public async Task<ActionResult<YarpConfiguration>> GetConfiguration()
    {
        var routes = await _adminService.GetRoutesAsync();
        var clusters = await _adminService.GetClustersAsync();
        
        return Ok(new YarpConfiguration
        {
            Routes = routes.ToList(),
            Clusters = clusters.ToList()
        });
    }

    /// <summary>
    /// Imports a complete YARP configuration.
    /// </summary>
    [HttpPost("config/import")]
    public async Task<ActionResult> ImportConfiguration([FromBody] YarpConfiguration config)
    {
        if (!_options.AllowConfigurationChanges)
            return StatusCode(403, new { message = "Configuration changes are not allowed" });

        foreach (var cluster in config.Clusters)
        {
            await _adminService.UpsertClusterAsync(cluster);
        }

        foreach (var route in config.Routes)
        {
            await _adminService.UpsertRouteAsync(route);
        }

        await _adminService.ApplyConfigurationAsync();
        
        return Ok(new { message = "Configuration imported successfully" });
    }

    #endregion
}
