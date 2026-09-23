using System;
using System.Collections.Generic;
using Application.Models;
using Persistence.Models.Interface;

namespace Persistence.Models;

/// <inheritdoc />
public class NetatmoWindPoint : IInfluxPoint
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
    public NetatmoWindPoint(string measurement)
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
        this.SetTags(device);
    }

    /// <inheritdoc />
    public void Initialize(Device device, StationModule module)
    {
        this.TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(module.DashboardData.TimeUtc).UtcDateTime;
        this.fields.Clear();
        this.SetFields(module.DashboardData);

        this.tags.Clear();
        this.SetTags(device, module);
    }
    
    /// <inheritdoc />
    public void SetFields(Dashboard dashboardData)
    {
        if (dashboardData.WindStrength.HasValue)
        {
            this.fields["wind_strength"] = dashboardData.WindStrength.Value;
        }

        if (dashboardData.WindAngle.HasValue)
        {
            this.fields["wind_angle"] = dashboardData.WindAngle.Value;
        }

        if (dashboardData.GustStrength.HasValue)
        {
            this.fields["gust_strength"] = dashboardData.GustStrength.Value;
        }

        if (dashboardData.GustAngle.HasValue)
        {
            this.fields["gust_angle"] = dashboardData.GustAngle.Value;
        }

        if (dashboardData.MaxWindStrength.HasValue)
        {
            this.fields["max_wind_str"] = dashboardData.MaxWindStrength.Value;
        }

        if (dashboardData.MaxWindAngle.HasValue)
        {
            this.fields["max_wind_angle"] = dashboardData.MaxWindAngle.Value;
        }

        if (dashboardData.DateMaxWindStrength.HasValue)
        {
            this.fields["date_max_wind_str"] = dashboardData.DateMaxWindStrength.Value;
        }

    }
    /// <inheritdoc />
    public void SetTags(Device device)
    {
        this.tags.Clear();
        this.tags["module_id"] = device.Id;
        this.tags["module_type"] = device.Type;
        this.tags["home_id"] = device.HomeId;
        this.tags["home_name"] = device.HomeName;
        this.tags["module_name"] = device.ModuleName;
        this.tags["room_id"] = device.RoomId;

        if (!string.IsNullOrWhiteSpace(device.RoomName))
        {
            this.tags["room_name"] = device.RoomName;
        }
    }

    /// <inheritdoc />
    public void SetTags(Device device, StationModule module)
    {
        this.tags.Clear();
        this.tags["module_id"] = module.Id;
        this.tags["module_type"] = module.Type;
        this.tags["home_id"] = device.HomeId;
        this.tags["home_name"] = device.HomeName;
        this.tags["module_name"] = module.ModuleName;
        this.tags["room_id"] = module.RoomId;

        if (!string.IsNullOrWhiteSpace(module.RoomName))
        {
            this.tags["room_name"] = module.RoomName;
        }
    }

}
