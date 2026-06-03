using System.Security.Claims;
using Layla.Api.Filters;
using Layla.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Layla.Core.Tests;

// ── helpers ───────────────────────────────────────────────────────────────────

file static class FilterContextFactory
{
    internal static ActionExecutingContext Make(
        ClaimsPrincipal? user = null,
        bool allowAnonymous = false)
    {
        var httpContext = new DefaultHttpContext();
        if (user != null) httpContext.User = user;

        var descriptor = new ActionDescriptor();
        if (allowAnonymous)
            descriptor.EndpointMetadata = [new AllowAnonymousAttribute()];

        var actionContext = new ActionContext(httpContext, new RouteData(), descriptor);
        return new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), new object());
    }

    internal static ClaimsPrincipal UserWithSubClaim(string userId) =>
        new(new ClaimsIdentity([new Claim(ClaimNames.Sub, userId)], "Bearer"));
}

// ── anonymous endpoint — filter is bypassed ───────────────────────────────────

public class RequireUserIdFilter_WhenEndpointIsAnonymous
{
    private readonly ActionExecutingContext _ctx;

    public RequireUserIdFilter_WhenEndpointIsAnonymous()
    {
        _ctx = FilterContextFactory.Make(allowAnonymous: true);
        new RequireUserIdFilter().OnActionExecuting(_ctx);
    }

    [Fact] public void ResultIsNotSet() => Assert.Null(_ctx.Result);
}

// ── no userId claim on authenticated endpoint — 401 ──────────────────────────

public class RequireUserIdFilter_WhenUserIdClaimIsMissing
{
    private readonly ActionExecutingContext _ctx;

    public RequireUserIdFilter_WhenUserIdClaimIsMissing()
    {
        // Authenticated principal but without any userId claim
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], "Bearer"));
        _ctx = FilterContextFactory.Make(principal);
        new RequireUserIdFilter().OnActionExecuting(_ctx);
    }

    [Fact] public void ResultIsUnauthorized() => Assert.IsType<UnauthorizedObjectResult>(_ctx.Result);
}

// ── valid userId claim — stored in HttpContext.Items ──────────────────────────

public class RequireUserIdFilter_WhenUserIdClaimIsPresent
{
    private readonly ActionExecutingContext _ctx;

    public RequireUserIdFilter_WhenUserIdClaimIsPresent()
    {
        _ctx = FilterContextFactory.Make(FilterContextFactory.UserWithSubClaim("user-123"));
        new RequireUserIdFilter().OnActionExecuting(_ctx);
    }

    [Fact] public void ResultIsNotSet() => Assert.Null(_ctx.Result);
    [Fact] public void UserIdStoredInHttpContextItems() =>
        Assert.Equal("user-123", _ctx.HttpContext.Items[HttpContextConstants.UserId]);
}
