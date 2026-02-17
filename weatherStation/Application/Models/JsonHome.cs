using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Application.Models;

/// <summary>
/// Root response payload from Netatmo <c>/api/homesdata</c>.
/// </summary>
public sealed class JsonHome : IValidatable
{
    /// <summary>
    /// The response body.
    /// </summary>
    public Body Body { get; init; } = new();

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
/// Body payload for a homesdata response.
/// </summary>
public sealed class Body : IValidatable
{
    /// <summary>
    /// List of homes visible to the user.
    /// </summary>
    public List<Home> Homes { get; init; } = [];

    /// <summary>
    /// User information.
    /// </summary>
    public User User { get; set; } = new();

    /// <inheritdoc />
    public bool IsValid()
    {
        if (this.Homes is null || this.Homes.Count == 0)
        {
            return false;
        }

        if (this.Homes.Any(home => !home.IsValid()))
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// A home containing rooms, modules, and schedules.
/// </summary>
public sealed class Home : IValidatable
{
    /// <summary>
    /// Id of the home.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Name of the home.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Altitude.
    /// </summary>
    public int Altitude { get; set; }

    /// <summary>
    /// Coordinates (longitude, latitude).
    /// </summary>
    public double[] Coordinates { get; set; } = [];

    /// <summary>
    /// Country code.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Timezone.
    /// </summary>
    public string Timezone { get; set; } = string.Empty;

    /// <summary>
    /// Rooms in the home.
    /// </summary>
    public List<Room> Rooms { get; set; } = new();

    /// <summary>
    /// Modules/devices associated with the home.
    /// </summary>
    public List<Module> Modules { get; set; } = new();

    /// <summary>
    /// Schedules configured for the home.
    /// </summary>
    public List<Schedule> Schedules { get; set; } = new();

    /// <inheritdoc />
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(this.Id))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(this.Name))
        {
            return false;
        }

        if (this.Rooms == null || this.Modules == null)
        {
            return false;
        }

        if (this.Rooms.Any(room => !room.IsValid()))
        {
            return false;
        }


        if (this.Modules.Any(module => !module.IsValid()))
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// A room in a home.
/// </summary>
public sealed class Room : IValidatable
{
    /// <summary>
    /// Room id.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Room name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Thermostat relay id for the room (if applicable).
    /// </summary>
    [JsonProperty("therm_relay")]
    public string ThermRelay { get; set; } = string.Empty;

    /// <summary>
    /// Room type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// List of module ids associated with the room.
    /// </summary>
    public List<string> ModuleIds { get; set; } = new();

    /// <summary>
    /// Validates that required fields are present and that module ids are well-formed.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(this.Id))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(this.Name))
        {
            return false;
        }

        if (this.ModuleIds == null)
        {
            return false;
        }
        if (this.ModuleIds.Any(moduleId => string.IsNullOrWhiteSpace(moduleId)))
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// A module/device associated with a home.
/// </summary>
public sealed class Module : IValidatable
{
    /// <summary>
    /// Module id.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Module type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Module name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unix timestamp when the module was set up.
    /// </summary>
    public long SetupDate { get; set; }

    /// <summary>
    /// Appliance type (when applicable).
    /// </summary>
    [JsonProperty("appliance_type")]
    public string ApplianceType { get; set; } = string.Empty;

    /// <summary>
    /// Room id that the module belongs to.
    /// </summary>
    public string RoomId { get; set; } = string.Empty;

    /// <summary>
    /// Ids of modules bridged by this module (when applicable).
    /// </summary>
    public List<string> ModulesBridged { get; set; } = new();

    /// <summary>
    /// Bridge id (when applicable).
    /// </summary>
    public string Bridge { get; set; } = string.Empty;

    /// <summary>
    /// Validates that required fields are present.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(this.Id))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(this.Name))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(this.Type))
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// User information returned by Netatmo in a homesdata response.
/// </summary>
public sealed class User : IValidatable
{
    /// <summary>
    /// User email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User language.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// User locale.
    /// </summary>
    public string Locale { get; set; } = string.Empty;

    /// <summary>
    /// Algorithm used to compute feel-like temperature.
    /// </summary>
    public int FeelLikeAlgorithm { get; set; }

    /// <summary>
    /// Pressure unit.
    /// </summary>
    public int UnitPressure { get; set; }

    /// <summary>
    /// Unit system.
    /// </summary>
    public int UnitSystem { get; set; }

    /// <summary>
    /// Wind unit.
    /// </summary>
    public int UnitWind { get; set; }

    /// <summary>
    /// User id.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user has pending consent to provide.
    /// </summary>
    [JsonProperty("pending_user_consent")]
    public bool PendingUserConsent { get; set; }

    /// <summary>
    /// User country code.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Validates that required fields are present.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(this.Id))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(this.Email))
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// Schedule configuration for a home.
/// </summary>
public sealed class Schedule
{
    /// <summary>
    /// Schedule id.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Schedule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this schedule is the default.
    /// </summary>
    [JsonProperty("default")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether this schedule is selected.
    /// </summary>
    public bool Selected { get; set; }

    /// <summary>
    /// Schedule type (for example <c>therm</c>, <c>event</c>, <c>electricity</c>).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Timetable entries.
    /// </summary>
    public List<TimetableEntry> Timetable { get; set; } = new();

    /// <summary>
    /// Zones for the schedule.
    /// </summary>
    public List<ScheduleZone> Zones { get; set; } = new();

    /// <summary>
    /// Away temperature (when applicable).
    /// </summary>
    [JsonProperty("away_temp")]
    public double? AwayTemp { get; set; }

    /// <summary>
    /// Frost guard temperature (when applicable).
    /// </summary>
    [JsonProperty("hg_temp")]
    public double? HgTemp { get; set; }

    /// <summary>
    /// Tariff (when applicable).
    /// </summary>
    public string Tariff { get; set; } = string.Empty;

    /// <summary>
    /// Tariff option (when applicable).
    /// </summary>
    [JsonProperty("tariff_option")]
    public string TariffOption { get; set; } = string.Empty;

    /// <summary>
    /// Power threshold (when applicable).
    /// </summary>
    [JsonProperty("power_threshold")]
    public double? PowerThreshold { get; set; }

    /// <summary>
    /// Contract power unit (when applicable).
    /// </summary>
    [JsonProperty("contract_power_unit")]
    public string ContractPowerUnit { get; set; } = string.Empty;

    /// <summary>
    /// Sunrise-based timetable entries (when applicable).
    /// </summary>
    [JsonProperty("timetable_sunrise")]
    public List<SunTimetableEntry> TimetableSunrise { get; set; } = new();

    /// <summary>
    /// Sunset-based timetable entries (when applicable).
    /// </summary>
    [JsonProperty("timetable_sunset")]
    public List<SunTimetableEntry> TimetableSunset { get; set; } = new();

    /// <summary>
    /// Additional fields not explicitly modeled.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, JToken> Extra { get; set; } = new Dictionary<string, JToken>();
}

/// <summary>
/// A single timetable entry within a schedule.
/// </summary>
public sealed class TimetableEntry
{
    /// <summary>
    /// Zone id.
    /// </summary>
    [JsonProperty("zone_id")]
    public int ZoneId { get; set; }

    /// <summary>
    /// Minute offset.
    /// </summary>
    [JsonProperty("m_offset")]
    public int MinuteOffset { get; set; }
}

/// <summary>
/// A timetable entry tied to sunrise or sunset.
/// </summary>
public sealed class SunTimetableEntry
{
    /// <summary>
    /// Zone id.
    /// </summary>
    [JsonProperty("zone_id")]
    public int ZoneId { get; set; }

    /// <summary>
    /// Day index.
    /// </summary>
    public int Day { get; set; }

    /// <summary>
    /// Twilight offset in minutes.
    /// </summary>
    [JsonProperty("twilight_offset")]
    public int TwilightOffset { get; set; }
}

/// <summary>
/// Zone configuration within a schedule.
/// </summary>
public sealed class ScheduleZone
{
    /// <summary>
    /// Zone name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Zone id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Zone type (when applicable).
    /// </summary>
    public int? Type { get; set; }

    /// <summary>
    /// Price type (for electricity schedules).
    /// </summary>
    [JsonProperty("price_type")]
    public string PriceType { get; set; } = string.Empty;

    /// <summary>
    /// Price value (for electricity schedules).
    /// </summary>
    [JsonProperty("price_value")]
    public double? PriceValue { get; set; }

    /// <summary>
    /// Rooms array. In the Netatmo schema this can contain different shapes depending on schedule type.
    /// Stored as raw JSON to support the documented variants.
    /// </summary>
    public JToken Rooms { get; set; }

    /// <summary>
    /// Modules array (event schedules). Stored as raw JSON to support the documented variants.
    /// </summary>
    public JToken Modules { get; set; }

    /// <summary>
    /// Additional fields not explicitly modeled.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, JToken> Extra { get; set; } = new Dictionary<string, JToken>();
}
