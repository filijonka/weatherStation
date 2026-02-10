using API.Auth.Netatmo.Options;
using API.Interface.Logic;

using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace API.Logic;

/// <inheritdoc />
public sealed class NetatmoLogicDataProvider : INetatmoLogicDataProvider
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IOptions<NetatmoOptions> options;
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetatmoLogicDataProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory for creating HTTP clients.</param>
    /// <param name="options">Netatmo options.</param>
    /// <param name="logger">Serilog logger.</param>
    public NetatmoLogicDataProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<NetatmoOptions> options,
        ILogger logger
    )
    {
        this.httpClientFactory = httpClientFactory;
        this.options = options;
        this.logger = logger.ForContext<NetatmoLogicDataProvider>();
    }

    /// <inheritdoc />
    public async Task<JsonElement> GetHomesDataAsync(
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

        using JsonDocument jsonDoc = JsonDocument.Parse(responseBody);
        return jsonDoc.RootElement.Clone();
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

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        bool isFirst = true;
        foreach (string gatewayType in gatewayTypes)
        {
            if (string.IsNullOrWhiteSpace(gatewayType))
            {
                continue;
            }

            if (!isFirst)
            {
                builder.Append('&');
            }

            builder.Append("gateway_types=");
            builder.Append(Uri.EscapeDataString(gatewayType));
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
}
