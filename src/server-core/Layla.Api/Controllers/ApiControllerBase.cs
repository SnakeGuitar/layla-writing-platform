using Layla.Api.Filters;
using Layla.Core.Common;
using Layla.Core.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Layla.Api.Controllers;

[ApiController]
[ServiceFilter(typeof(RequireUserIdFilter))]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Returns the authenticated user's ID, pre-validated by <see cref="RequireUserIdFilter"/>.
    /// Only safe to call inside actions decorated with [Authorize] (filter skips anonymous endpoints).
    /// </summary>
    protected string CurrentUserId => HttpContext.Items[HttpContextConstants.UserId] as string ?? string.Empty;

    protected ActionResult RespondWithError(ErrorCode? errorCode) =>
        (errorCode?.GetStatusCode() ?? 500) switch
        {
            401 => Unauthorized(new { Error = errorCode?.GetMessage() }),
            403 => Forbid(),
            404 => NotFound(new { Error = errorCode?.GetMessage() }),
            409 => Conflict(new { Error = errorCode?.GetMessage() }),
            423 => StatusCode(StatusCodes.Status423Locked, new { Error = errorCode?.GetMessage() }),
            500 => StatusCode(StatusCodes.Status500InternalServerError, new { Error = errorCode?.GetMessage() }),
            _ => BadRequest(new { Error = errorCode?.GetMessage() })
        };
}
