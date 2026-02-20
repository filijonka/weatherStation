using System;
using System.Collections.Generic;
using Application.Models;
using Persistence.Models.Interface;

namespace Persistence.Models;

/// <inheritdoc />
public class NetatmoRainPoint : IInfluxPoint
{
    /// <inheritdoc />
    private readonly Dictionary<string, string> tags;

    /// <inheritdoc />
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
    /// Current rain amount.
    /// </summary>
    public double? Rain { get; private set; }

    /// <summary>
    /// Rain amount for the last hour.
    /// </summary>
    public double? SumRain1 { get; private set; }

    /// <summary>
    /// Rain amount for the last 24 hours.
    /// </summary>
    public double? SumRain24 { get; private set; }

    /// <inheritdoc />
    public void Initialize(Device device)
    {
        this.TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(device.DashboardData.TimeUtc).UtcDateTime;

        this.Rain = device.DashboardData.Rain;
        this.SumRain1 = device.DashboardData.SumRain1;
        this.SumRain24 = device.DashboardData.SumRain24;

        this.tags.Clear();
        this.tags["device_id"] = device.Id;
        this.tags["device_type"] = device.Type;
        this.tags["home_id"] = device.HomeId;
        this.tags["home_name"] = device.HomeName;
        this.tags["module_name"] = device.ModuleName;

        this.fields.Clear();

        if (this.Rain.HasValue)
        {
            this.fields["rain"] = this.Rain.Value;
        }

        if (this.SumRain1.HasValue)
        {
            this.fields["sum_rain_1"] = this.SumRain1.Value;
        }

        if (this.SumRain24.HasValue)
        {
            this.fields["sum_rain_24"] = this.SumRain24.Value;
        }
    }

    private void InitializeModule(StationModule module)
    {
        this.TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(module.DashboardData.TimeUtc).UtcDateTime;

        this.Rain = module.DashboardData.Rain;
        this.SumRain1 = module.DashboardData.SumRain1;
        this.SumRain24 = module.DashboardData.SumRain24;

        this.tags.Clear();
        this.tags["module_id"] = module.Id;
        this.tags["module_type"] = module.Type;
        this.tags["module_name"] = module.ModuleName;

        this.fields.Clear();

        if (this.Rain.HasValue)
        {
            this.fields["rain"] = this.Rain.Value;
        }

        if (this.SumRain1.HasValue)
        {
            this.fields["sum_rain_1"] = this.SumRain1.Value;
        }

        if (this.SumRain24.HasValue)
        {
            this.fields["sum_rain_24"] = this.SumRain24.Value;
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
    /// Construtor with measurement
    /// </summary>
    /// <param name="measurement">Influx measurement name.</param>
    public NetatmoRainPoint(string measurement)
    {
        this.Measurement = measurement;
        this.TimestampUtc = DateTime.UnixEpoch;
        this.tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        this.fields = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }
}