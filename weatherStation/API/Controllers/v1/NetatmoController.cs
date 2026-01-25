using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Options;
using API.Auth.Netatmo.Services;

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly NetatmoOptions options;
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetatmoController"/> class.
    /// </summary>
    /// <param name="oauthClient">OAuth client for Netatmo.</param>
    /// <param name="tokenStore">Token store.</param>
    /// <param name="options">Netatmo options.</param>
    /// <param name="logger">Logger.</param>
    public NetatmoController(
        INetatmoOAuthClient oauthClient,
        INetatmoTokenStore tokenStore,
        IOptions<NetatmoOptions> options,
        ILogger logger
    )
    {
        this.oauthClient = oauthClient;
        this.tokenStore = tokenStore;
        this.options = options.Value;
        this.logger = logger;
    }

    /// <summary>
    /// Returns Netatmo authentication status for use by Grafana (and other UIs).
    /// When not authenticated, <see cref="NetatmoStatusResponse.LoginUrl"/> points to the login flow.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Status payload with authenticated, status, loginUrl, and expiresAtUtc.</returns>
    [HttpGet("status")]
    public async Task<ActionResult<NetatmoStatusResponse>> StatusAsync(CancellationToken cancellationToken)
    {
        NetatmoTokenInfo info = await this.tokenStore.LoadAsync(cancellationToken);
        bool authenticated = info != null && info.ExpiresAtUtc > DateTime.UtcNow;
        string status = authenticated ? "Connected" : "Login required";

        string baseUrl = (this.options.ApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            baseUrl = $"{this.Request.Scheme}://{this.Request.Host}";
        }

        string? loginUrl = authenticated ? null : $"{baseUrl}/api/v1/netatmo/login";
        DateTime? expiresAtUtc = authenticated ? info!.ExpiresAtUtc : null;

        return this.Ok(new NetatmoStatusResponse(authenticated, status, loginUrl, expiresAtUtc));
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

            NetatmoCallbackResponse response = new NetatmoCallbackResponse(
                tokenInfo.Scope,
                tokenInfo.ExpiresAtUtc
            );

            return this.Ok(response);
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
    /// Netatmo callback response payload.
    /// </summary>
    /// <param name="Scope">Granted scopes.</param>
    /// <param name="ExpiresAtUtc">Token expiry time (UTC).</param>
    public sealed record NetatmoCallbackResponse(string Scope, DateTime ExpiresAtUtc);

    /// <summary>
    /// Netatmo auth status for UIs (e.g. Grafana). When not authenticated,
    /// <see cref="LoginUrl"/> is the URL to start the OAuth login flow.
    /// </summary>
    /// <param name="Authenticated">True if a valid token exists and is not expired.</param>
    /// <param name="Status">Human-readable status: "Connected" or "Login required".</param>
    /// <param name="LoginUrl">URL to log in; null when authenticated.</param>
    /// <param name="ExpiresAtUtc">Token expiry (UTC); null when not authenticated.</param>
    public sealed record NetatmoStatusResponse(bool Authenticated, string Status, string? LoginUrl, DateTime? ExpiresAtUtc);
}
