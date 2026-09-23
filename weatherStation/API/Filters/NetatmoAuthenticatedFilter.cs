using API.Auth.Netatmo.Interface;
using API.Helpers;
using API.Responses;
using Application.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading;
using System.Threading.Tasks;

namespace API.Filters;

/// <summary>
/// Action filter that checks Netatmo authentication and exposes the access token via HttpContext.Items.
/// </summary>
public class NetatmoAuthenticatedFilter : IAsyncActionFilter
{
    /// <summary>
    /// the token name
    /// </summary>
    public const string HttpContextItemAccessTokenKey = "NetatmoAccessToken";

    private readonly INetatmoTokenStore tokenStore;
    private readonly INetatmoOAuthClient netatmoOAuthClient;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tokenStore"></param>
    /// <param name="netatmoOAuthClient"></param>
    public NetatmoAuthenticatedFilter(
        INetatmoTokenStore tokenStore,
        INetatmoOAuthClient netatmoOAuthClient
    )
    {
        this.tokenStore = tokenStore;
        this.netatmoOAuthClient = netatmoOAuthClient;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        CancellationToken cancellationToken = context.HttpContext.RequestAborted;
        HttpRequest request = context.HttpContext.Request;

        string baseUrl = $"{request.Scheme}://{request.Host}";
        NetatmoAuthStatusResponse authStatus = await this.netatmoOAuthClient.Login(cancellationToken);

        if (!authStatus.Authenticated)
        {
            context.Result = new UnauthorizedObjectResult(new ApiResponse<JsonHome>
            {
                IsSuccess = false,
                IsAuthenticated = false,
                LoginUrl = authStatus.LoginUrl,
                Error = "Not authenticated.",
                Data = new JsonHome()
            });
            return;
        }

        context.HttpContext.Items[HttpContextItemAccessTokenKey] = authStatus.Token;

        await next();
    }
}