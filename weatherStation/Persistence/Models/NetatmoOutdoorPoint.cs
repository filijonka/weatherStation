using System;
using System.Collections.Generic;
using Application.Models;
using Persistence.Models.Interface;

namespace Persistence.Models;

/// <inheritdoc />
public class NetatmoOutdoorPoint : IInfluxPoint
{
    private readonly Dictionary<string, string> tags;
    private readonly Dictionary<string, object> fields;

    /// <inheritdoc />
    public string Measurement { get; }

    /// <inheritdoc />
    public DateTime TimestampUtc { get; private set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Tags => this.tags;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Fields => this.fields;

    /// <summary>
    /// Outdoor temperature.
    /// </summary>
    public double? Temperature { get; private set; }

    /// <summary>
    /// Outdoor humidity.
    /// </summary>
    public double? Humidity { get; private set; }

    /// <summary>
    /// Minimum measured temperature.
    /// </summary>
    public double? MinTemp { get; private set; }

    /// <summary>
    /// Maximum measured temperature.
    /// </summary>
    public double? MaxTemp { get; private set; }

    /// <summary>
    /// Timestamp for maximum temperature.
    /// </summary>
    public long? DateMaxTemp { get; private set; }

    /// <summary>
    /// Timestamp for minimum temperature.
    /// </summary>
    public long? DateMinTemp { get; private set; }

    /// <summary>
    /// Temperature trend.
    /// </summary>
    public string TempTrend { get; private set; }

    /// <inheritdoc />
    public void Initialize(Device device)
    {
        this.TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(device.DashboardData.TimeUtc).UtcDateTime;

        this.Temperature = device.DashboardData.Temperature;
        this.Humidity = device.DashboardData.Humidity;
        this.MinTemp = device.DashboardData.MinTemp;
        this.MaxTemp = device.DashboardData.MaxTemp;
        this.DateMaxTemp = device.DashboardData.DateMaxTemp;
        this.DateMinTemp = device.DashboardData.DateMinTemp;
        this.TempTrend = device.DashboardData.TempTrend;

        this.tags.Clear();
        this.tags["device_id"] = device.Id;
        this.tags["device_type"] = device.Type;
        this.tags["home_id"] = device.HomeId;
        this.tags["home_name"] = device.HomeName;
        this.tags["module_name"] = device.ModuleName;

        this.fields.Clear();

        if (this.Temperature.HasValue)
        {
            this.fields["temperature"] = this.Temperature.Value;
        }

        if (this.Humidity.HasValue)
        {
            this.fields["humidity"] = this.Humidity.Value;
        }

        if (this.MinTemp.HasValue)
        {
            this.fields["min_temp"] = this.MinTemp.Value;
        }

        if (this.MaxTemp.HasValue)
        {
            this.fields["max_temp"] = this.MaxTemp.Value;
        }

        if (this.DateMaxTemp.HasValue)
        {
            this.fields["date_max_temp"] = this.DateMaxTemp.Value;
        }

        if (this.DateMinTemp.HasValue)
        {
            this.fields["date_min_temp"] = this.DateMinTemp.Value;
        }

        if (!string.IsNullOrWhiteSpace(this.TempTrend))
        {
            this.fields["temp_trend"] = this.TempTrend;
        }
    }

    private void InitializeModule(StationModule module)
    {
        this.TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(module.DashboardData.TimeUtc).UtcDateTime;

        this.Temperature = module.DashboardData.Temperature;
        this.Humidity = module.DashboardData.Humidity;
        this.MinTemp = module.DashboardData.MinTemp;
        this.MaxTemp = module.DashboardData.MaxTemp;
        this.DateMaxTemp = module.DashboardData.DateMaxTemp;
        this.DateMinTemp = module.DashboardData.DateMinTemp;
        this.TempTrend = module.DashboardData.TempTrend;

        this.tags.Clear();
        this.tags["module_id"] = module.Id;
        this.tags["module_type"] = module.Type;
        this.tags["module_name"] = module.ModuleName;

        this.fields.Clear();

        if (this.Temperature.HasValue)
        {
            this.fields["temperature"] = this.Temperature.Value;
        }

        if (this.Humidity.HasValue)
        {
            this.fields["humidity"] = this.Humidity.Value;
        }

        if (this.MinTemp.HasValue)
        {
            this.fields["min_temp"] = this.MinTemp.Value;
        }

        if (this.MaxTemp.HasValue)
        {
            this.fields["max_temp"] = this.MaxTemp.Value;
        }

        if (this.DateMaxTemp.HasValue)
        {
            this.fields["date_max_temp"] = this.DateMaxTemp.Value;
        }

        if (this.DateMinTemp.HasValue)
        {
            this.fields["date_min_temp"] = this.DateMinTemp.Value;
        }

        if (!string.IsNullOrWhiteSpace(this.TempTrend))
        {
            this.fields["temp_trend"] = this.TempTrend;
        }
    }

    /// <inheritdoc />
    public void Initialize(StationModule module)
    {
        this.InitializeModule(module);
        this.tags["room_id"] = module.RoomId;

        if (!string.IsNullOrWhiteSpace(module.RoomName))
        {
            this.tags["room_name"] = module.RoomName;
        }
    }

    /// <summary>
    /// Constructor with only measurement
    /// </summary>
    /// <param name="measurement">Influx measurement name.</param>
    public NetatmoOutdoorPoint(string measurement)
    {
        this.Measurement = measurement;
        this.TimestampUtc = DateTime.UnixEpoch;
        this.tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        this.fields = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        this.TempTrend = string.Empty;
    }

    /// <summary>
    /// Constructor with the basedata
    /// </summary>
    /// <param name="measurement">Influx measurement name.</param>
    /// <param name="timestampUtc">Point timestamp in UTC.</param>
    /// <param name="tags">Point tags.</param>
    /// <param name="fields">Point fields.</param>
    public NetatmoOutdoorPoint(string measurement, DateTime timestampUtc, IReadOnlyDictionary<string, string> tags, IReadOnlyDictionary<string, object> fields)
    {
        this.Measurement = measurement;
        this.TimestampUtc = timestampUtc;
        this.tags = new Dictionary<string, string>(tags, StringComparer.OrdinalIgnoreCase);
        this.fields = new Dictionary<string, object>(fields, StringComparer.OrdinalIgnoreCase);
        this.TempTrend = string.Empty;
    }
}