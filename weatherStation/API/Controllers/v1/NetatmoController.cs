using API.Auth.Netatmo.Interface;
using API.Interface.Logic;
using API.Exceptions;

using Asp.Versioning;
using Application.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using API.Filters;

namespace API.Controllers.v1;

/// <summary>
/// Netatmo OAuth endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/netatmo")]
public class NetatmoController : ControllerBase
{
    private readonly INetatmoTokenStore tokenStore;
    private readonly INetatmoLogicDataProvider logicDataProvider;
    private readonly ILogger logger;

    private static readonly Regex ModuleIdRegex = new Regex(
        "^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){5}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="NetatmoController"/> class.
    /// </summary>
    /// <param name="tokenStore">Token store.</param>
    /// <param name="logicDataProvider">Logic data provider for Netatmo API calls.</param>
    /// <param name="logger">Logger.</param>
    public NetatmoController(
        INetatmoTokenStore tokenStore,
        INetatmoLogicDataProvider logicDataProvider,
        ILogger logger
    )
    {
        this.tokenStore = tokenStore;
        this.logicDataProvider = logicDataProvider;
        this.logger = logger;
    }

    /// <summary>
    /// Fetches home data from Netatmo API.
    /// </summary>
    /// <param name="gatewayTypes">Optional gateway types filter (comma-separated: NLG, OTH, NBG, BNMH, BNS).</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Home data JSON response.</returns>
    [HttpGet("homesdata")]
    [NetatmoAuthenticated]
    public async Task<ActionResult<ApiResponse<JsonHome>>> GetHomesDataAsync(
        [FromQuery] string gatewayTypes,
        CancellationToken cancellationToken
    )
    {
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
            string accessToken = (string)HttpContext.Items[NetatmoAuthenticatedFilter.HttpContextItemAccessTokenKey]!;

            JsonHome jsonHome = await this.logicDataProvider.GetHomesDataAsync(
                accessToken,
                gatewayTypesArray,
                cancellationToken
            );

            if (jsonHome == null || !jsonHome.IsValid())
            {
                this.logger.Warning("Netatmo homesdata returned an empty payload.");
                return this.StatusCode(502, new ApiResponse<JsonHome>
                {
                    IsSuccess = false,
                    IsAuthenticated = true,
                    LoginUrl = string.Empty,
                    Error = "No home data was returned. Please try again later.",
                    Data = new JsonHome()
                });
            }

            return this.Ok(new ApiResponse<JsonHome>
            {
                IsSuccess = true,
                IsAuthenticated = true,
                LoginUrl = string.Empty,
                Error = string.Empty,
                Data = jsonHome
            });
        }
        catch (Exception ex)
        {
            this.logger.Error(ex, "Failed to fetch Netatmo homesdata: {Message}", ex.Message);
            return this.StatusCode(502, new ApiResponse<JsonHome>
            {
                IsSuccess = false,
                IsAuthenticated = true,
                LoginUrl = string.Empty,
                Error = "Failed to retrieve home data from Netatmo. Please try again later.",
                Data = new JsonHome()
            });
        }


    }

    /// <summary>
    /// Fetches station/module data from Netatmo API.
    /// </summary>
    /// <param name="moduleId">Module/device id (MAC-like format).</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Station/module data response.</returns>
    [HttpGet("moduledata/{moduleId}")]
    [NetatmoAuthenticated]
    public async Task<ActionResult<ApiResponse<JsonStationData>>> GetModuleDataAsync(
        [FromRoute] string moduleId,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(moduleId) || !ModuleIdRegex.IsMatch(moduleId))
        {
            return this.BadRequest(new ApiResponse<JsonStationData>
            {
                IsSuccess = false,
                IsAuthenticated = true,
                LoginUrl = string.Empty,
                Error = "Invalid module id format.",
                Data = new JsonStationData()
            });
        }

        try
        {
            string accessToken = (string)HttpContext.Items[NetatmoAuthenticatedFilter.HttpContextItemAccessTokenKey]!;
            JsonStationData jsonStationData = await this.logicDataProvider.GetModuleDataAsync(
                accessToken,
                moduleId,
                cancellationToken
            );

            if (jsonStationData == null || !jsonStationData.IsValid())
            {
                this.logger.Warning("Netatmo getstationsdata returned an empty payload.");
                return this.StatusCode(502, new ApiResponse<JsonStationData>
                {
                    IsSuccess = false,
                    IsAuthenticated = true,
                    LoginUrl = string.Empty,
                    Error = "No station data was returned. Please try again later.",
                    Data = new JsonStationData()
                });
            }

            return this.Ok(new ApiResponse<JsonStationData>
            {
                IsSuccess = true,
                IsAuthenticated = true,
                LoginUrl = string.Empty,
                Error = string.Empty,
                Data = jsonStationData
            });
        }
        catch (NetatmoBadRequestException ex)
        {
            this.logger.Error(ex, "Netatmo getstationsdata returned 400: {Code} {Message}", ex.Code, ex.ApiMessage);
            return this.BadRequest(new ApiResponse<JsonStationData>
            {
                IsSuccess = false,
                IsAuthenticated = true,
                LoginUrl = string.Empty,
                Error = ex.ApiMessage,
                Data = new JsonStationData()
            });
        }
        catch (Exception ex)
        {
            this.logger.Error(ex, "Failed to fetch Netatmo getstationsdata: {Message}", ex.Message);
            return this.StatusCode(502, new ApiResponse<JsonStationData>
            {
                IsSuccess = false,
                IsAuthenticated = true,
                LoginUrl = string.Empty,
                Error = "Failed to retrieve station data from Netatmo. Please try again later.",
                Data = new JsonStationData()
            });
        }
    }
    
    
}
