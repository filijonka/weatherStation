using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Services;

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Threading;
using System;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="NetatmoController"/> class.
    /// </summary>
    /// <param name="oauthClient">OAuth client for Netatmo.</param>
    /// <param name="tokenStore">Token store.</param>
    public NetatmoController(
        INetatmoOAuthClient oauthClient,
        INetatmoTokenStore tokenStore
    )
    {
        this.oauthClient = oauthClient;
        this.tokenStore = tokenStore;
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
    public async Task<ActionResult<NetatmoCallbackResponse>> CallbackAsync(
        [FromQuery] string code,
        CancellationToken cancellationToken
    )
    {
        NetatmoTokenInfo tokenInfo = await this.oauthClient.ExchangeCodeAsync(code, cancellationToken);
        await this.tokenStore.SaveAsync(tokenInfo, cancellationToken);

        NetatmoCallbackResponse response = new NetatmoCallbackResponse(
            tokenInfo.Scope,
            tokenInfo.ExpiresAtUtc
        );

        return this.Ok(response);
    }

    /// <summary>
    /// Netatmo callback response payload.
    /// </summary>
    /// <param name="Scope">Granted scopes.</param>
    /// <param name="ExpiresAtUtc">Token expiry time (UTC).</param>
    public sealed record NetatmoCallbackResponse(string Scope, DateTime ExpiresAtUtc);
}
