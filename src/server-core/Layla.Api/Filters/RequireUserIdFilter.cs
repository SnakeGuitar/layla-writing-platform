using Layla.Api.Extensions;
using Layla.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Layla.Api.Filters;

/// <summary>
/// Action filter that validates the user ID claim from the JWT token.
/// Skips anonymous endpoints automatically.
/// Stores the resolved user ID in <c>HttpContext.Items</c> for use by the action.
/// Returns 401 Unauthorized immediately if the claim is absent or empty on an authenticated endpoint.
/// </summary>
public class RequireUserIdFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var isAnonymous = context.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any();

        if (isAnonymous)
            return;

        var userId = context.HttpContext.User.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new UnauthorizedObjectResult(new { Error = "User ID not found in token." });
            return;
        }

        context.HttpContext.Items[HttpContextConstants.UserId] = userId;
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
