using System.Text.Json;
using API.Interface;
using API.Options;
using Microsoft.Extensions.Options;
using Serilog;
using WeatherStation.Persistence.Models;
using ILogger = Serilog.ILogger;

namespace API.Services;

public sealed class ExternalWeatherClient : IExternalWeatherClient
{
    private readonly HttpClient httpClient;
    private readonly WeatherApiOptions options;
    private readonly ILogger logger;

    public ExternalWeatherClient(
        HttpClient httpClient,
        IOptions<WeatherApiOptions> options,
        ILogger logger
    )
    {
        this.httpClient = httpClient;
        this.options = options.Value;
        this.logger = logger.ForContext<ExternalWeatherClient>();
    }

    public async Task<IReadOnlyCollection<WeatherReading>> GetReadingsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(this.options.BaseUrl))
        {
            this.logger.Warning("Weather API base URL is not configured.");
            return Array.Empty<WeatherReading>();
        }

        Uri baseUri = new Uri(this.options.BaseUrl, UriKind.Absolute);
        Uri requestUri = new Uri(baseUri, this.options.ReadingsPath);

        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        if (!string.IsNullOrWhiteSpace(this.options.ApiKey))
        {
            request.Headers.Add(this.options.ApiKeyHeaderName, this.options.ApiKey);
        }

        using HttpResponseMessage response = await this.httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            this.logger.Warning("Weather API returned {StatusCode}", response.StatusCode);
            return Array.Empty<WeatherReading>();
        }

        await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        List<ExternalWeatherReading>? payload = await JsonSerializer.DeserializeAsync<List<ExternalWeatherReading>>(
            contentStream,
            serializerOptions,
            cancellationToken
        );

        if (payload == null || payload.Count == 0)
        {
            return Array.Empty<WeatherReading>();
        }

        List<WeatherReading> readings = new List<WeatherReading>(payload.Count);
        foreach (ExternalWeatherReading item in payload)
        {
            if (!ExternalWeatherReadingMapper.TryMap(item, out WeatherReading reading))
            {
                this.logger.Warning("Skipping invalid reading from external API.");
                continue;
            }

            readings.Add(reading);
        }

        return readings;
    }

    
}
