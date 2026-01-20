namespace API.Interface;

public interface IWeatherIngestService
{
    Task<int> FetchAndStoreAsync(CancellationToken cancellationToken);
}
