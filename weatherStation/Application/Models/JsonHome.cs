using System.Collections.Generic;
using System.Linq;

namespace Application.Models;

public sealed class JsonHome : IValidatable
{
    public Body Body { get; init; } = new();
    public string Status { get; set; } = string.Empty;
    public double TimeExec { get; set; }
    public long TimeServer { get; set; }

    public bool IsValid()
    {
        return this.Body != null && this.Body.IsValid();
    }
}

public sealed class Body : IValidatable
{
    public List<Home> Homes { get; init; } = [];
    public User User { get; set; } = new();

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

public sealed class Home : IValidatable
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Altitude { get; set; }
    public double[] Coordinates { get; set; } = [];
    public string Country { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;

    public List<Room> Rooms { get; set; } = new();
    public List<Module> Modules { get; set; } = new();

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

public sealed class Room : IValidatable
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> ModuleIds { get; set; } = new();

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

public sealed class Module : IValidatable
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long SetupDate { get; set; }
    public string RoomId { get; set; } = string.Empty;
    public List<string> ModulesBridged { get; set; } = new();
    public string Bridge { get; set; } = string.Empty;

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

public sealed class User : IValidatable
{
    public string Email { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public int FeelLikeAlgorithm { get; set; }
    public int UnitPressure { get; set; }
    public int UnitSystem { get; set; }
    public int UnitWind { get; set; }
    public string Id { get; set; } = string.Empty;

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
