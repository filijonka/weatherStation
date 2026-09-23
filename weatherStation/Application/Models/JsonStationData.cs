using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Application.Models;

/// <summary>
/// Root response payload from Netatmo <c>/api/getstationsdata</c>.
/// </summary>
public sealed class JsonStationData : IValidatable
{
    /// <summary>
    /// The response body.
    /// </summary>
    public StationBody Body { get; init; } = new();

    /// <summary>
    /// Response status (for example <c>ok</c>).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Amount of time the execution took (seconds).
    /// </summary>
    public double TimeExec { get; set; }

    /// <summary>
    /// Server time as a Unix timestamp.
    /// </summary>
    public long TimeServer { get; set; }

    /// <inheritdoc />
    public bool IsValid()
    {
        return this.Body != null && this.Body.IsValid();
    }
}

/// <summary>
/// The body
/// </summary>
public sealed class StationBody : IValidatable
{
    /// <summary>
    /// The list of base station devices.
    /// </summary>
    public List<Device> Devices { get; init; } = [];

    /// <summary>
    /// The user associated with the devices.
    /// </summary>
    public StationUser User { get; set; } = new();

    /// <inheritdoc />
    public bool IsValid()
    {
        if (this.Devices is null || this.Devices.Count == 0)
        {
            return false;
        }

        if (this.Devices.Any(device => device is null || !device.IsValid()))
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// A device
/// </summary>
public sealed class Device : IValidatable
{
    /// <summary>
    /// MAC address of the device.
    /// </summary>
    [JsonProperty("_id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Unix timestamp when the weather station was set up.
    /// </summary>
    [JsonProperty("date_setup")]
    public long DateSetup { get; set; }

    /// <summary>
    /// Unix timestamp of the last installation.
    /// </summary>
    [JsonProperty("last_setup")]
    public long LastSetup { get; set; }

    /// <summary>
    /// Unix timestamp of the last status update.
    /// </summary>
    [JsonProperty("last_status_store")]
    public long LastStatusStore { get; set; }

    /// <summary>
    /// Type of the device (for example <c>NAMain</c>).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Name of the module.
    /// </summary>
    [JsonProperty("module_name")]
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Firmware version.
    /// </summary>
    [JsonProperty("firmware")]
    public int Firmware { get; set; }

    /// <summary>
    /// Unix timestamp of the last upgrade.
    /// </summary>
    [JsonProperty("last_upgrade")]
    public long LastUpgrade { get; set; }

    /// <summary>
    /// Wi-Fi status per base station (example: 86=bad, 56=good).
    /// </summary>
    [JsonProperty("wifi_status")]
    public int WifiStatus { get; set; }

    /// <summary>
    /// True if the station connected to Netatmo cloud within the last 4 hours.
    /// </summary>
    public bool Reachable { get; set; }

    /// <summary>
    /// True if the station is calibrating the CO2 sensor.
    /// </summary>
    [JsonProperty("co2_calibrating")]
    public bool Co2Calibrating { get; set; }

    /// <summary>
    /// Array of data measured by the device (e.g. Temperature, Humidity).
    /// </summary>
    [JsonProperty("data_type")]
    public string[] DataType { get; set; } = [];

    /// <summary>
    /// Location information for the station.
    /// </summary>
    public StationPlace Place { get; set; } = new();

    /// <summary>
    /// True if the user owns the station, false if they are invited.
    /// </summary>
    [JsonProperty("read_only")]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Id of the home where the station is placed.
    /// </summary>
    [JsonProperty("home_id")]
    public string HomeId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the home where the station is placed.
    /// </summary>
    [JsonProperty("home_name")]
    public string HomeName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the station.
    /// Note: Netatmo indicates this is deprecated in favor of <c>home_name</c> and <c>module_name</c>.
    /// </summary>
    [JsonProperty("station_name")]
    public string StationName { get; set; } = string.Empty;

    /// <summary>
    /// Latest measured values for the device.
    /// </summary>
    [JsonProperty("dashboard_data")]
    public Dashboard DashboardData { get; set; } = new Dashboard();

    /// <summary>
    /// A list of modules attached to this base station.
    /// </summary>
    public List<StationModule> Modules { get; set; } = new List<StationModule>();

    /// <summary>
    /// Extra properties we don't have members for in the class
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JToken> Extra { get; init; } = new();

    /// <summary>
    /// Room id where the module is located.
    /// This value is enriched from homes data during ingestion and is not part of getstationsdata payload.
    /// </summary>
    [JsonIgnore]
    public string RoomId { get; set; } = string.Empty;

    /// <summary>
    /// Room name where the module is located.
    /// This value is enriched from homes data during ingestion and is not part of getstationsdata payload.
    /// </summary>
    [JsonIgnore]
    public string RoomName { get; set; } = string.Empty;

    /// <inheritdoc />
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(this.Id))
        {
            return false;
        }

        if (this.DashboardData is null || !this.DashboardData.IsValid())
        {
            return false;
        }

        if (this.Modules is null)
        {
            return false;
        }

        if (this.Modules.Any(module => module is null || !module.IsValid()))
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// Place information for a station.
/// </summary>
public sealed class StationPlace
{
    /// <summary>
    /// Timezone.
    /// </summary>
    public string Timezone { get; set; } = string.Empty;

    /// <summary>
    /// Country code.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// City name (may be omitted depending on API/account).
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Altitude.
    /// </summary>
    public int Altitude { get; set; }

    /// <summary>
    /// Latitude and longitude.
    /// </summary>
    public double[] Location { get; set; } = [];
}

/// <summary>
/// Latest measured values (varies by device/module type).
/// </summary>
public sealed class Dashboard : IValidatable
{
    /// <summary>
    /// Timestamp when data was measured (Unix seconds).
    /// </summary>
    [JsonProperty("time_utc")]
    public long TimeUtc { get; set; }

    /// <summary>
    /// Temperature (°C).
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// CO2 (ppm).
    /// </summary>
    [JsonProperty("CO2")]
    public double? Co2 { get; set; }

    /// <summary>
    /// Humidity (%).
    /// </summary>
    public double? Humidity { get; set; }

    /// <summary>
    /// Noise (dB).
    /// </summary>
    public double? Noise { get; set; }

    /// <summary>
    /// Surface pressure (mbar).
    /// </summary>
    public double? Pressure { get; init; }

    /// <summary>
    /// Sea-level pressure (mbar).
    /// </summary>
    public double? AbsolutePressure { get; set; }

    /// <summary>
    /// Min temperature
    /// </summary>
    [JsonProperty("min_temp")]
    public double? MinTemp { get; set; }

    /// <summary>
    /// Max temperature
    /// </summary>
    [JsonProperty("max_temp")]
    public double? MaxTemp { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("date_max_temp")]
    public long? DateMaxTemp { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("date_min_temp")]
    public long? DateMinTemp { get; set; }

    /// <summary>
    /// Trend of temperature
    /// </summary>
    [JsonProperty("temp_trend")]
    public string TempTrend { get; set; } = string.Empty;

    /// <summary>
    /// Trend of pressure
    /// </summary>
    [JsonProperty("pressure_trend")]
    public string PressureTrend { get; set; } = string.Empty;

    /// <summary>
    /// Rain (mm).
    /// </summary>
    public double? Rain { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("sum_rain_1")]
    public double? SumRain1 { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("sum_rain_24")]
    public double? SumRain24 { get; set; }

    /// <summary>
    /// Wind strength.
    /// </summary>
    public double? WindStrength { get; set; }

    /// <summary>
    /// Wind angle.
    /// </summary>
    public double? WindAngle { get; set; }

    /// <summary>
    /// Gust strength.
    /// </summary>
    public double? GustStrength { get; set; }

    /// <summary>
    /// Gust angle.
    /// </summary>
    public double? GustAngle { get; set; }

    /// <summary>
    /// Max wind strength
    /// </summary>
    [JsonProperty("max_wind_str")]
    public double? MaxWindStrength { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("max_wind_angle")]
    public double? MaxWindAngle { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("date_max_wind_str")]
    public long? DateMaxWindStrength { get; set; }

    /// <summary>
    /// Used for missing or ignored tags
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, JToken> Extra { get; set; } = new Dictionary<string, JToken>();

    /// <inheritdoc />
    public bool IsValid()
    {
        if (this.TimeUtc > 0)
        {
            return true;
        }

        if (this.Temperature.HasValue || this.Co2.HasValue || this.Humidity.HasValue)
        {
            return true;
        }

        if (this.Pressure.HasValue || this.AbsolutePressure.HasValue || this.Noise.HasValue)
        {
            return true;
        }

        if (this.Rain.HasValue || this.SumRain1.HasValue || this.SumRain24.HasValue)
        {
            return true;
        }

        if (this.WindStrength.HasValue || this.GustStrength.HasValue)
        {
            return true;
        }

        return this.Extra != null && this.Extra.Count > 0;
    }
}

/// <summary>
/// Module attached to a base station (e.g. outdoor module, rain gauge, wind module).
/// </summary>
public sealed class StationModule : IValidatable
{
    /// <summary>
    /// MAC address of the module.
    /// </summary>
    [JsonProperty("_id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type of the module (for example <c>NAModule1</c>, <c>NAModule2</c>).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Name of the module.
    /// </summary>
    [JsonProperty("module_name")]
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Array of data measured by the module.
    /// </summary>
    [JsonProperty("data_type")]
    public string[] DataType { get; set; } = [];

    /// <summary>
    /// Unix timestamp of the last installation.
    /// </summary>
    [JsonProperty("last_setup")]
    public long LastSetup { get; set; }

    /// <summary>
    /// Room id where the module is located.
    /// This value is enriched from homes data during ingestion and is not part of getstationsdata payload.
    /// </summary>
    [JsonIgnore]
    public string RoomId { get; set; } = string.Empty;

    /// <summary>
    /// Room name where the module is located.
    /// This value is enriched from homes data during ingestion and is not part of getstationsdata payload.
    /// </summary>
    [JsonIgnore]
    public string RoomName { get; set; } = string.Empty;

    /// <summary>
    /// True if the module connected to Netatmo cloud within the last 4 hours.
    /// </summary>
    public bool Reachable { get; set; }

    /// <summary>
    /// Firmware version.
    /// </summary>
    [JsonProperty("firmware")]
    public int Firmware { get; set; }

    /// <summary>
    /// Unix timestamp of the last measure update.
    /// </summary>
    [JsonProperty("last_message")]
    public long LastMessage { get; set; }

    /// <summary>
    /// Unix timestamp of the last status update.
    /// </summary>
    [JsonProperty("last_seen")]
    public long LastSeen { get; set; }

    /// <summary>
    /// Current radio status per module.
    /// </summary>
    [JsonProperty("rf_status")]
    public int RfStatus { get; set; }

    /// <summary>
    /// Current battery voltage (raw value, unit depends on module).
    /// </summary>
    [JsonProperty("battery_vp")]
    public int BatteryVp { get; set; }

    /// <summary>
    /// Percentage of battery remaining.
    /// </summary>
    [JsonProperty("battery_percent")]
    public int BatteryPercent { get; set; }

    /// <summary>
    /// Latest measured values for the module.
    /// </summary>
    [JsonProperty("dashboard_data")]
    public Dashboard DashboardData { get; set; } = new Dashboard();

    /// <summary>
    /// Extra properties we don't have members for in the class.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JToken> Extra { get; init; } = new();

    /// <inheritdoc />
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(this.Id))
        {
            return false;
        }

        if (this.DashboardData is null || !this.DashboardData.IsValid())
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// User information returned by Netatmo for station data.
/// </summary>
public sealed class StationUser
{
    /// <summary>
    /// User e-mail address.
    /// </summary>
    public string Mail { get; set; } = string.Empty;

    /// <summary>
    /// User administrative settings.
    /// </summary>
    public StationAdministrative Administrative { get; set; } = new();
}

/// <summary>
/// User administrative settings.
/// </summary>
public sealed class StationAdministrative
{
    /// <summary>
    /// User locale.
    /// </summary>
    public string Lang { get; set; } = string.Empty;

    /// <summary>
    /// User regional preferences (used for displaying date).
    /// </summary>
    public string RegLocale { get; set; } = string.Empty;

    /// <summary>
    /// Country code.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// 0 -> metric system, 1 -> imperial system.
    /// </summary>
    public int Unit { get; set; }

    /// <summary>
    /// 0 -> kph, 1 -> mph, 2 -> ms, 3 -> beaufort, 4 -> knot.
    /// </summary>
    public int Windunit { get; set; }

    /// <summary>
    /// 0 -> mbar, 1 -> inHg, 2 -> mmHg.
    /// </summary>
    public int Pressureunit { get; set; }

    /// <summary>
    /// Algorithm used to compute feel like temperature.
    /// 0 -> humidex, 1 -> heat-index.
    /// </summary>
    public int FeelLikeAlgo { get; set; }
}
