namespace API.Options;

public class WeatherApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ReadingsPath { get; set; } = "readings";

    public string ApiKeyHeaderName { get; set; } = "X-Api-Key";
}
