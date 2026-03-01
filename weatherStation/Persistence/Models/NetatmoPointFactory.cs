using Persistence.Models.Interface;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Persistence.Influx;

namespace Persistence.Models;

/// <summary>
/// Facory class for creating a Point
/// </summary>
public sealed class NetatmoPointFactory
{
    private readonly string measurement;
    private readonly IOptions<InfluxOptions> options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="measurement"> used as fallback table name if configuration is missing</param>
    public NetatmoPointFactory(IOptions<InfluxOptions> options, string measurement)
    {
        this.options = options;
        this.measurement = measurement;
    }

    /// <summary>
    /// Creation of correct object based on type
    /// </summary>
    /// <param name="netatmoType">Netatmo module type identifier.</param>
    /// <returns>A concrete point implementation matching the Netatmo type.</returns>
    public IInfluxPoint Create(string netatmoType)
    {
        
        Dictionary<string, string> tables = options.Value.Tables;
        string table = tables.GetValueOrDefault(netatmoType, this.measurement);
        return netatmoType switch
        {
            "NAMain" => new NetatmoIndoorPoint(table),
            "NAModule1" => new NetatmoOutdoorPoint(table),
            "NAModule2" => new NetatmoWindPoint(table),
            "NAModule3" => new NetatmoRainPoint(table),
            "NAModule4" => new NetatmoIndoorPoint(table),
            _ => throw new ArgumentOutOfRangeException(nameof(netatmoType), netatmoType,
                "Unsupported Netatmo module type.")
        };
    }
}