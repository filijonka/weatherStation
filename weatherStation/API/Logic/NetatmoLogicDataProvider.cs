using API.Auth.Netatmo.Options;
using API.Exceptions;
using API.Interface.Logic;
using Application.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace API.Logic;

/// <inheritdoc />
public sealed class NetatmoLogicDataProvider : INetatmoLogicDataProvider
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IOptions<NetatmoOptions> options;
    private readonly IMemoryCache memoryCache;
    private readonly ILogger logger;

    private const string ModuleRoomCacheKey = "netatmo:module-room-map";

    /// <summary>
    /// Initializes a new instance of the <see cref="NetatmoLogicDataProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory for creating HTTP clients.</param>
    /// <param name="options">Netatmo options.</param>
    /// <param name="memoryCache"></param>
    /// <param name="logger">Serilog logger.</param>
    public NetatmoLogicDataProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<NetatmoOptions> options,
        IMemoryCache memoryCache,
        ILogger logger
    )
    {
        this.httpClientFactory = httpClientFactory;
        this.options = options;
        this.memoryCache = memoryCache;
        this.logger = logger.ForContext<NetatmoLogicDataProvider>();
    }

    /// <inheritdoc />
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
    public async Task<JsonHome> GetHomesDataAsync(
        string accessToken,
        string[] gatewayTypes,
        CancellationToken cancellationToken
    )
    {
        string apiBaseUrl = GetApiBaseUrl();
        string endpoint = $"{apiBaseUrl}/api/homesdata";

        string queryString = BuildHomesDataQueryString(gatewayTypes);
        if (!string.IsNullOrEmpty(queryString))
        {
            endpoint = $"{endpoint}?{queryString}";
        }

        using HttpClient httpClient = this.httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await httpClient.GetAsync(endpoint, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            this.logger.Warning("Netatmo homesdata request failed: {StatusCode} {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException($"Netatmo API request failed with status {response.StatusCode}: {responseBody}");
        }

        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };

        JsonHome jsonHome = JsonConvert.DeserializeObject<JsonHome>(responseBody, settings);
        if (jsonHome == null)
        {
            throw new InvalidOperationException("Failed to deserialize Netatmo homesdata response.");
        }
        this.memoryCache.Remove(ModuleRoomCacheKey);
        return jsonHome;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ModuleRoomInfo> GetModuleRoomMap(JsonHome homes)
    {
        if (this.memoryCache.TryGetValue(ModuleRoomCacheKey, out IReadOnlyDictionary<string, ModuleRoomInfo> cached) &&
            cached != null)
        {
            return cached;
        }
        
        Dictionary<string, ModuleRoomInfo> map = BuildModuleRoomMap(homes);
        
        this.memoryCache.Set(ModuleRoomCacheKey, map);

        return map;
    }

    /// <inheritdoc />
    public async Task<JsonStationData> GetModuleDataAsync(
        string accessToken,
        string moduleId,
        CancellationToken cancellationToken
    )
    {
        string apiBaseUrl = GetApiBaseUrl();
        string endpoint = $"{apiBaseUrl}/api/getstationsdata?device_id={Uri.EscapeDataString(moduleId)}";

        using HttpClient httpClient = this.httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await httpClient.GetAsync(endpoint, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            try
            {
                NetatmoError netatmoError = JsonConvert.DeserializeObject<NetatmoError>(responseBody);

                var test = netatmoError.ErrorType;
                this.logger.Error(
                    "Netatmo returned 400. Code: {Code}. Message: {Message}",
                    netatmoError.ErrorType.Code,
                    netatmoError.ErrorType.Message
                );

                throw new NetatmoBadRequestException(netatmoError.ErrorType.Code, netatmoError.ErrorType.Message);
            }
            catch (JsonException ex)
            {
                this.logger.Warning(ex, "Failed to deserialize Netatmo 400 error payload. Body: {Body}", responseBody);
                throw new NetatmoBadRequestException(-1, "Netatmo request was rejected.");
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            this.logger.Warning(
                "Netatmo getstationsdata request failed: {StatusCode} {Body}",
                response.StatusCode,
                responseBody
            );
            throw new HttpRequestException($"Netatmo API request failed with status {response.StatusCode}: {responseBody}");
        }

        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };

        JsonStationData jsonStationData = JsonConvert.DeserializeObject<JsonStationData>(responseBody, settings);
        if (jsonStationData == null)
        {
            throw new InvalidOperationException("Failed to deserialize Netatmo getstationsdata response.");
        }

        return jsonStationData;
    }

    /// <summary>
    /// Builds query string for homesdata endpoint with gateway_types parameters.
    /// </summary>
    /// <param name="gatewayTypes">Array of gateway types.</param>
    /// <returns>Query string (without leading ?).</returns>
    private static string BuildHomesDataQueryString(string[] gatewayTypes)
    {
        if (gatewayTypes == null || gatewayTypes.Length == 0)
        {
            return string.Empty;
        }

        HashSet<string> allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            "NLG",
            "OTH",
            "NBG",
            "BNMH",
            "BNS"
        };

        HashSet<string> selected = new(StringComparer.OrdinalIgnoreCase);

        foreach (string gatewayType in gatewayTypes)
        {
            if (string.IsNullOrWhiteSpace(gatewayType))
            {
                continue;
            }

            string normalized = gatewayType.Trim();

            // Basic length guard to avoid absurdly long values.
            if (normalized.Length > 16)
            {
                continue;
            }

            if (!allowed.Contains(normalized))
            {
                continue;
            }

            selected.Add(normalized.ToUpperInvariant());

            // Safety cap
            if (selected.Count >= 10)
            {
                break;
            }
        }

        if (selected.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        bool isFirst = true;

        foreach (string gt in selected)
        {
            if (!isFirst)
            {
                builder.Append('&');
            }

            builder.Append("gateway_types=");
            builder.Append(Uri.EscapeDataString(gt));
            isFirst = false;
        }
        return builder.ToString();
    }

    /// <summary>
    /// Gets the Netatmo API base URL from TokenUrl (e.g., https://api.netatmo.com/oauth2/token -> https://api.netatmo.com).
    /// </summary>
    /// <returns>Base URL for Netatmo API.</returns>
    private string GetApiBaseUrl()
    {
        if (Uri.TryCreate(this.options.Value.TokenUrl, UriKind.Absolute, out Uri tokenUri))
        {
            return $"{tokenUri.Scheme}://{tokenUri.Host}";
        }

        return "https://api.netatmo.com";
    }
    
    private static Dictionary<string, ModuleRoomInfo> BuildModuleRoomMap(JsonHome homes)
    {
        Dictionary<string, ModuleRoomInfo> result = new Dictionary<string, ModuleRoomInfo>();

        foreach (Home home in homes.Body.Homes)
        {
            Dictionary<string, string> roomNameById = new Dictionary<string, string>();
            foreach (Room room in home.Rooms.Where(room => !string.IsNullOrWhiteSpace(room.Id)))
            {
                roomNameById[room.Id] = room.Name ?? string.Empty;
            }

            foreach (Module module in home.Modules.Where(module => !string.IsNullOrEmpty(module.Id)))
            {
                string roomId = module.RoomId ?? string.Empty;
                string roomName = roomNameById.TryGetValue(roomId, out string name) ? name : string.Empty;

                result[module.Id] = new ModuleRoomInfo(roomId, roomName);
            }
        }

        return result;
    }
}
