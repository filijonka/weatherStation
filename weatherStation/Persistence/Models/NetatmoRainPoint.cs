using System;
using System.Collections.Generic;
using Application.Models;
using Persistence.Models.Interface;

namespace Persistence.Models;

/// <inheritdoc />
public class NetatmoRainPoint : IInfluxPoint
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

    /// <inheritdoc />
    public void Initialize(Device device)
    {
        this.TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(device.DashboardData.TimeUtc).UtcDateTime;

        this.fields.Clear();
        this.SetFields(device.DashboardData);
        this.tags.Clear();
        this.tags["device_id"] = device.Id;
        this.tags["device_type"] = device.Type;
        this.tags["home_id"] = device.HomeId;
        this.tags["home_name"] = device.HomeName;
        this.tags["module_name"] = device.ModuleName;

    }

    /// <inheritdoc />
    public void Initialize(StationModule module)
    {
        this.TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(module.DashboardData.TimeUtc).UtcDateTime;
        this.fields.Clear();
        this.SetFields(module.DashboardData);

        this.tags.Clear();
        this.tags["module_id"] = module.Id;
        this.tags["module_type"] = module.Type;
        this.tags["module_name"] = module.ModuleName;
        
        this.tags["room_id"] = module.RoomId;

        if (!string.IsNullOrWhiteSpace(module.RoomName))
        {
            this.tags["room_name"] = module.RoomName;
        }
    }

    /// <inheritdoc />
    public void SetFields(Dashboard dashboardData)
    {
        if (dashboardData.Rain.HasValue)
        {
            this.fields["rain"] = dashboardData.Rain.Value;
        }

        if (dashboardData.SumRain1.HasValue)
        {
            this.fields["sum_rain_1"] = dashboardData.SumRain1.Value;
        }

        if (dashboardData.SumRain24.HasValue)
        {
            this.fields["sum_rain_24"] = dashboardData.SumRain24.Value;
        }

    }
}
