using Layla.Core.Common;
using Layla.Core.Contracts.Auth;
using Layla.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Layla.Api.Controllers
{
    /// <summary>
    /// Issues JWT access tokens in exchange for valid credentials.
    /// </summary>
    [Route("api/[controller]")]
    public class TokensController : ApiControllerBase
    {
        private readonly IAuthService _authService;

        /// <summary>Initialises the controller with the authentication service.</summary>
        public TokensController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Authenticate with email and password to receive a JWT Bearer token.
        /// </summary>
        /// <remarks>
        /// The returned token is valid for 24 hours and must be sent in the
        /// <c>Authorization: Bearer &lt;token&gt;</c> header on subsequent requests.
        /// Each successful login increments the user's <c>TokenVersion</c>, invalidating
        /// all previously issued tokens for that account.
        /// </remarks>
        /// <param name="request">Login credentials.</param>
        /// <response code="200">Authentication successful. Returns the JWT and basic user info.</response>
        /// <response code="401">Invalid credentials.</response>
        /// <response code="423">Account is locked after too many failed login attempts.</response>
        [HttpPost]
        [EnableRateLimiting("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status423Locked)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<AuthResponseDto>> CreateToken(LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);

            if (!result.IsSuccess)
                return RespondWithError(result.ErrorCode);

            return Ok(result.Data);
        }
    }
}
