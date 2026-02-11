using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Options;
using API.Helpers;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using API.Auth.Netatmo.Services;
using API.Responses;

namespace API.Controllers.v1;

/// <summary>
/// Health check endpoints.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
[ApiController]
public class HealthController : ControllerBase
{
    private readonly INetatmoTokenStore tokenStore;
    private readonly IOptions<NetatmoOptions> options;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthController"/> class.
    /// </summary>
    /// <param name="tokenStore">Token store.</param>
    /// <param name="options">Netatmo options.</param>
    public HealthController(
        INetatmoTokenStore tokenStore,
        IOptions<NetatmoOptions> options
    )
    {
        this.tokenStore = tokenStore;
        this.options = options;
    }

    /// <summary>
    /// Returns service health status.
    /// </summary>
    /// <returns>Health response payload.</returns>
    [HttpGet("health")]
    public ActionResult GetHealth()
    {
        return this.Ok("Healthy");
    }

    /// <summary>
    /// Returns Netatmo authentication status for use by Grafana (and other UIs).
    /// When not authenticated, loginUrl points to the login flow.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Status payload with authenticated, status, loginUrl, and expiresAtUtc.</returns>
    [HttpGet("netatmo")]
    public async Task<ActionResult<NetatmoAuthStatusResponse>> GetNetatmoStatusAsync(CancellationToken cancellationToken)
    {
        string baseUrl = (this.options.Value.ApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            baseUrl = $"{this.Request.Scheme}://{this.Request.Host}";
        }

        return await NetatmoAuthHelper.GetAuthStatusAsync(
            this.tokenStore,
            baseUrl,
            cancellationToken
        );

    }
}
