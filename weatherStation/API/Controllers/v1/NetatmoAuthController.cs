using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Services;

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.Controllers.v1;

/// <summary>
/// Netatmo OAuth endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/netatmo")]
public sealed class NetatmoAuthController : ControllerBase
{
    private readonly INetatmoOAuthClient oauthClient;
    private readonly INetatmoTokenStore tokenStore;
    private readonly ILogger logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="oauthClient"></param>
    /// <param name="tokenStore"></param>
    /// <param name="logger"></param>
    public NetatmoAuthController(
        INetatmoOAuthClient oauthClient,
        INetatmoTokenStore tokenStore,
        ILogger logger
    )
    {
        this.oauthClient = oauthClient;
        this.tokenStore = tokenStore;
        this.logger = logger;
    }

    /// <summary>
    /// Starts the Netatmo OAuth login flow.
    /// </summary>
    /// <returns>Redirect result to Netatmo authorization URL.</returns>
    [HttpGet("auth/login")]
    public ActionResult Login()
    {
        string state = Guid.NewGuid().ToString("N");
        string authorizeUrl = this.oauthClient.BuildAuthorizeUrl(state);
        return this.Redirect(authorizeUrl);
    }

    /// <summary>
    /// OAuth callback endpoint that exchanges the authorization code for tokens.
    /// </summary>
    /// <param name="code">Authorization code from Netatmo.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Callback response payload.</returns>
    [HttpGet("callback")]
    [HttpGet("auth/callback")]
    public async Task<ActionResult> CallbackAsync(
        [FromQuery] string code,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return this.BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Detail = "The authorization code is missing or invalid."
            });
        }

        try
        {
            NetatmoTokenInfo tokenInfo = await this.oauthClient.ExchangeCodeAsync(code, cancellationToken);
            await this.tokenStore.SaveAsync(tokenInfo, cancellationToken);
            return new JsonResult(new { scope = tokenInfo.Scope, expiresAtUtc = tokenInfo.ExpiresAtUtc });
        }
        catch (Exception ex)
        {
            this.logger.Error(ex, "Netatmo callback failed: {Message}", ex.Message);
            return this.StatusCode(502, new ProblemDetails
            {
                Title = "Sign-in Failed",
                Detail = "The sign-in with Netatmo could not be completed. Please try again."
            });
        }
    }
}
