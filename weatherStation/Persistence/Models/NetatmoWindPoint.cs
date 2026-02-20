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
    /// Wind strength.
    /// </summary>
    public double? WindStrength { get; private set; }

    /// <summary>
    /// Wind direction angle.
    /// </summary>
    public double? WindAngle { get; private set; }

    /// <summary>
    /// Gust strength.
    /// </summary>
    public double? GustStrength { get; private set; }

    /// <summary>
    /// Gust direction angle.
    /// </summary>
    public double? GustAngle { get; private set; }

    /// <summary>
    /// Maximum wind strength.
    /// </summary>
    public double? MaxWindStrength { get; private set; }

    /// <summary>
    /// Direction for maximum wind strength.
    /// </summary>
    public double? MaxWindAngle { get; private set; }

    /// <summary>
    /// Timestamp of maximum wind strength.
    /// </summary>
    public long? DateMaxWindStrength { get; private set; }

    /// <inheritdoc />
    public void Initialize(Device device)
    {
        this.TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(device.DashboardData.TimeUtc).UtcDateTime;

        this.WindStrength = device.DashboardData.WindStrength;
        this.WindAngle = device.DashboardData.WindAngle;
        this.GustStrength = device.DashboardData.GustStrength;
        this.GustAngle = device.DashboardData.GustAngle;
        this.MaxWindStrength = device.DashboardData.MaxWindStrength;
        this.MaxWindAngle = device.DashboardData.MaxWindAngle;
        this.DateMaxWindStrength = device.DashboardData.DateMaxWindStrength;

        this.tags.Clear();
        this.tags["device_id"] = device.Id;
        this.tags["device_type"] = device.Type;
        this.tags["home_id"] = device.HomeId;
        this.tags["home_name"] = device.HomeName;
        this.tags["module_name"] = device.ModuleName;

        this.fields.Clear();

        if (this.WindStrength.HasValue)
        {
            this.fields["wind_strength"] = this.WindStrength.Value;
        }

        if (this.WindAngle.HasValue)
        {
            this.fields["wind_angle"] = this.WindAngle.Value;
        }

        if (this.GustStrength.HasValue)
        {
            this.fields["gust_strength"] = this.GustStrength.Value;
        }

        if (this.GustAngle.HasValue)
        {
            this.fields["gust_angle"] = this.GustAngle.Value;
        }

        if (this.MaxWindStrength.HasValue)
        {
            this.fields["max_wind_str"] = this.MaxWindStrength.Value;
        }

        if (this.MaxWindAngle.HasValue)
        {
            this.fields["max_wind_angle"] = this.MaxWindAngle.Value;
        }

        if (this.DateMaxWindStrength.HasValue)
        {
            this.fields["date_max_wind_str"] = this.DateMaxWindStrength.Value;
        }
    }

    private void InitializeModule(StationModule module)
    {
        this.TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(module.DashboardData.TimeUtc).UtcDateTime;

        this.WindStrength = module.DashboardData.WindStrength;
        this.WindAngle = module.DashboardData.WindAngle;
        this.GustStrength = module.DashboardData.GustStrength;
        this.GustAngle = module.DashboardData.GustAngle;
        this.MaxWindStrength = module.DashboardData.MaxWindStrength;
        this.MaxWindAngle = module.DashboardData.MaxWindAngle;
        this.DateMaxWindStrength = module.DashboardData.DateMaxWindStrength;

        this.tags.Clear();
        this.tags["module_id"] = module.Id;
        this.tags["module_type"] = module.Type;
        this.tags["module_name"] = module.ModuleName;

        this.fields.Clear();

        if (this.WindStrength.HasValue)
        {
            this.fields["wind_strength"] = this.WindStrength.Value;
        }

        if (this.WindAngle.HasValue)
        {
            this.fields["wind_angle"] = this.WindAngle.Value;
        }

        if (this.GustStrength.HasValue)
        {
            this.fields["gust_strength"] = this.GustStrength.Value;
        }

        if (this.GustAngle.HasValue)
        {
            this.fields["gust_angle"] = this.GustAngle.Value;
        }

        if (this.MaxWindStrength.HasValue)
        {
            this.fields["max_wind_str"] = this.MaxWindStrength.Value;
        }

        if (this.MaxWindAngle.HasValue)
        {
            this.fields["max_wind_angle"] = this.MaxWindAngle.Value;
        }

        if (this.DateMaxWindStrength.HasValue)
        {
            this.fields["date_max_wind_str"] = this.DateMaxWindStrength.Value;
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
    public NetatmoWindPoint(string measurement)
    {
        this.Measurement = measurement;
        this.TimestampUtc = DateTime.UnixEpoch;
        this.tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        this.fields = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }
}