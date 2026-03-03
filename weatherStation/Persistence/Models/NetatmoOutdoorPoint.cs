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
    /// Constructor with only measurement
    /// </summary>
    /// <param name="measurement">Influx measurement name.</param>
    public NetatmoOutdoorPoint(string measurement)
    {
        this.Measurement = measurement;
        this.TimestampUtc = DateTime.UnixEpoch;
        this.tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        this.fields = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Constructor with the basedata
    /// </summary>
    /// <param name="measurement">Influx measurement name.</param>
    /// <param name="timestampUtc">Point timestamp in UTC.</param>
    /// <param name="tags">Point tags.</param>
    /// <param name="fields">Point fields.</param>
    public NetatmoOutdoorPoint(
        string measurement,
        DateTime timestampUtc,
        IReadOnlyDictionary<string, string> tags,
        IReadOnlyDictionary<string, object> fields
    )
    {
        this.Measurement = measurement;
        this.TimestampUtc = timestampUtc;
        this.tags = new Dictionary<string, string>(tags, StringComparer.OrdinalIgnoreCase);
        this.fields = new Dictionary<string, object>(fields, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public void Initialize(Device device)
    {
        this.TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(device.DashboardData.TimeUtc).UtcDateTime;

        this.fields.Clear();
        SetFields(device.DashboardData);

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
        if (dashboardData.Temperature.HasValue)
        {
            this.fields["temperature"] = dashboardData.Temperature.Value;
        }

        if (dashboardData.Humidity.HasValue)
        {
            this.fields["humidity"] = dashboardData.Humidity.Value;
        }

        if (dashboardData.MinTemp.HasValue)
        {
            this.fields["min_temp"] = dashboardData.MinTemp.Value;
        }

        if (dashboardData.MaxTemp.HasValue)
        {
            this.fields["max_temp"] = dashboardData.MaxTemp.Value;
        }

        if (dashboardData.DateMaxTemp.HasValue)
        {
            this.fields["date_max_temp"] = dashboardData.DateMaxTemp.Value;
        }

        if (dashboardData.DateMinTemp.HasValue)
        {
            this.fields["date_min_temp"] = dashboardData.DateMinTemp.Value;
        }

        if (!string.IsNullOrWhiteSpace(dashboardData.TempTrend))
        {
            this.fields["temp_trend"] = dashboardData.TempTrend;
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
