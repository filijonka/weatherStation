using Persistence.Models.Interface;
using System;

namespace Persistence.Models;

/// <summary>
/// Facory class for creating a Point
/// </summary>
public sealed class NetatmoPointFactory
{
    private readonly string measurement;

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="measurement">Influx measurement name to apply to created points.</param>
    public NetatmoPointFactory(string measurement)
    {
        this.measurement = measurement;
    }

    /// <summary>
    /// Creation of correct object based on type
    /// </summary>
    /// <param name="netatmoType">Netatmo module type identifier.</param>
    /// <returns>A concrete point implementation matching the Netatmo type.</returns>
    public IInfluxPoint Create(string netatmoType)
    {
        return netatmoType switch
        {
            "NAMain" => new NetatmoIndoorPoint(this.measurement),
            "NAModule1" => new NetatmoOutdoorPoint(this.measurement),
            "NAModule2" => new NetatmoWindPoint(this.measurement),
            "NAModule3" => new NetatmoRainPoint(this.measurement),
            "NAModule4" => new NetatmoIndoorPoint(this.measurement),
            _ => throw new ArgumentOutOfRangeException(nameof(netatmoType), netatmoType, "Unsupported Netatmo module type.")
        };
    }
}