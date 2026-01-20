using InfluxDB.Client;

namespace WeatherStation.Persistence.Influx;

public interface IInfluxClientFactory
{
    InfluxDBClient GetClient();
}
