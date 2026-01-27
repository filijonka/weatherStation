using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Services;
using API.Helpers;
using API.Interface.Logic;

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using API.Responses;
using Serilog;

namespace API.Controllers.v1;

/// <summary>
/// Netatmo OAuth endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/netatmo")]
public sealed class NetatmoController : ControllerBase
{
    private readonly INetatmoOAuthClient oauthClient;
    private readonly INetatmoTokenStore tokenStore;
    private readonly INetatmoLogicDataProvider logicDataProvider;
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetatmoController"/> class.
    /// </summary>
    /// <param name="oauthClient">OAuth client for Netatmo.</param>
    /// <param name="tokenStore">Token store.</param>
    /// <param name="logicDataProvider">Logic data provider for Netatmo API calls.</param>
    /// <param name="logger">Logger.</param>
    public NetatmoController(
        INetatmoOAuthClient oauthClient,
        INetatmoTokenStore tokenStore,
        INetatmoLogicDataProvider logicDataProvider,
        ILogger logger
    )
    {
        this.oauthClient = oauthClient;
        this.tokenStore = tokenStore;
        this.logicDataProvider = logicDataProvider;
        this.logger = logger;
    }

    /// <summary>
    /// Starts the Netatmo OAuth login flow.
    /// </summary>
    /// <returns>Redirect result to Netatmo authorization URL.</returns>
    [HttpGet("login")]
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

    /// <summary>
    /// Fetches home data from Netatmo API.
    /// </summary>
    /// <param name="gatewayTypes">Optional gateway types filter (comma-separated: NLG, OTH, NBG, BNMH, BNS).</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Home data JSON response.</returns>
    [HttpGet("homesdata")]
    public async Task<ActionResult<NetatmoAuthStatusResponse>> GetHomesDataAsync(
        [FromQuery] string gatewayTypes,
        CancellationToken cancellationToken
    )
    {
        string baseUrl = $"{this.Request.Scheme}://{this.Request.Host}";
        NetatmoAuthStatusResponse authStatus = await NetatmoAuthHelper.GetAuthStatusAsync(
            this.tokenStore,
            baseUrl,
            cancellationToken
        );

        if (!authStatus.Authenticated)
        {
            return authStatus;
        }

        string[] gatewayTypesArray = null;
        if (!string.IsNullOrWhiteSpace(gatewayTypes))
        {
            gatewayTypesArray = gatewayTypes
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(gt => gt.Trim())
                .Where(gt => !string.IsNullOrWhiteSpace(gt))
                .ToArray();
        }

        try
        {
            JsonElement jsonElement = await this.logicDataProvider.GetHomesDataAsync(
                authStatus.Token,
                gatewayTypesArray,
                cancellationToken
            );

            return new JsonResult(jsonElement);
        }
        catch (Exception ex)
        {
            this.logger.Error(ex, "Failed to fetch Netatmo homesdata: {Message}", ex.Message);
            return this.StatusCode(502, new ProblemDetails
            {
                Title = "Netatmo API Error",
                Detail = "Failed to retrieve home data from Netatmo. Please try again later."
            });
        }
    }
}
