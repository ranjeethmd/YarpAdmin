using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using YarpAdmin;
using YarpAdmin.Middleware;

namespace YarpAdmin.Tests;

public class YarpAdminAuthMiddlewareTests
{
    private readonly Mock<ILogger<YarpAdminAuthMiddleware>> _mockLogger;
    private readonly YarpAdminOptions _options;

    public YarpAdminAuthMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<YarpAdminAuthMiddleware>>();
        _options = new YarpAdminOptions();
    }

    [Fact]
    public async Task InvokeAsync_NonAdminPath_CallsNext()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new YarpAdminAuthMiddleware(next, _options, _mockLogger.Object);
        var context = CreateHttpContext("/api/other");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_AdminApiPath_RequireAuthFalse_CallsNext()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        _options.RequireAuthentication = false;
        var middleware = new YarpAdminAuthMiddleware(next, _options, _mockLogger.Object);
        var context = CreateHttpContext("/api/yarp-admin/routes");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_AdminUiPath_RequireAuthFalse_CallsNext()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        _options.RequireAuthentication = false;
        var middleware = new YarpAdminAuthMiddleware(next, _options, _mockLogger.Object);
        var context = CreateHttpContext("/yarp-admin");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_AdminPath_RequireAuthTrue_Unauthenticated_Returns401()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        _options.RequireAuthentication = true;
        var middleware = new YarpAdminAuthMiddleware(next, _options, _mockLogger.Object);
        var context = CreateHttpContext("/api/yarp-admin/routes", authenticated: false);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AdminPath_RequireAuthTrue_Authenticated_CallsNext()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        _options.RequireAuthentication = true;
        var middleware = new YarpAdminAuthMiddleware(next, _options, _mockLogger.Object);
        var context = CreateHttpContext("/api/yarp-admin/routes", authenticated: true);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_AdminPath_WithPolicy_Authorized_CallsNext()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        _options.RequireAuthentication = true;
        _options.AuthenticationPolicy = "AdminPolicy";

        var mockAuthService = new Mock<IAuthorizationService>();
        mockAuthService.Setup(s => s.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<object?>(),
            "AdminPolicy"))
            .ReturnsAsync(AuthorizationResult.Success());

        var middleware = new YarpAdminAuthMiddleware(next, _options, _mockLogger.Object);
        var context = CreateHttpContext("/api/yarp-admin/routes", authenticated: true, authService: mockAuthService.Object);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_AdminPath_WithPolicy_Unauthorized_Returns403()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        _options.RequireAuthentication = true;
        _options.AuthenticationPolicy = "AdminPolicy";

        var mockAuthService = new Mock<IAuthorizationService>();
        mockAuthService.Setup(s => s.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<object?>(),
            "AdminPolicy"))
            .ReturnsAsync(AuthorizationResult.Failed());

        var middleware = new YarpAdminAuthMiddleware(next, _options, _mockLogger.Object);
        var context = CreateHttpContext("/api/yarp-admin/routes", authenticated: true, authService: mockAuthService.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    private static HttpContext CreateHttpContext(
        string path,
        bool authenticated = false,
        IAuthorizationService? authService = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "GET";
        context.Response.Body = new MemoryStream();

        if (authenticated)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser")
            }, "TestAuth");
            context.User = new ClaimsPrincipal(identity);
        }

        if (authService != null)
        {
            var services = new ServiceCollection();
            services.AddSingleton(authService);
            context.RequestServices = services.BuildServiceProvider();
        }

        return context;
    }
}

public class YarpAdminLoggingMiddlewareTests
{
    private readonly Mock<ILogger<YarpAdminLoggingMiddleware>> _mockLogger;

    public YarpAdminLoggingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<YarpAdminLoggingMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_AdminApiPath_LogsRequest()
    {
        var nextCalled = false;
        RequestDelegate next = context =>
        {
            nextCalled = true;
            context.Response.StatusCode = 200;
            return Task.CompletedTask;
        };
        var middleware = new YarpAdminLoggingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext("/api/yarp-admin/routes");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("YARP Admin API")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_NonAdminPath_DoesNotLog()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new YarpAdminLoggingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext("/api/other");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("YARP Admin API")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_AdminApiPath_LogsEvenOnException()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("Test exception");
        var middleware = new YarpAdminLoggingMiddleware(next, _mockLogger.Object);
        var context = CreateHttpContext("/api/yarp-admin/routes");

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("YARP Admin API")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static HttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "GET";
        return context;
    }
}
