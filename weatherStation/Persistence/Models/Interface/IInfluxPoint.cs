using System;
using System.Collections.Generic;
using Application.Models;

namespace Persistence.Models.Interface;

/// <summary>
/// General interface for a datapoint
/// </summary>
public interface IInfluxPoint
{
    /// <summary>
    /// The Influx measurement name
    /// </summary>
    string Measurement { get; }

    /// <summary>
    /// UTC timestamp for the point.
    /// </summary>
    DateTime TimestampUtc { get; }

    /// <summary>
    /// Indexed metadata used to filter/group in queries.
    /// </summary>
    IReadOnlyDictionary<string, string> Tags { get; }

    /// <summary>
    /// Measured values (typically numeric) stored as fields.
    /// </summary>
    IReadOnlyDictionary<string, object> Fields { get; }

    /// <summary>
    /// Read in a device and populate the object
    /// </summary>
    /// <param name="device">Source Netatmo device containing dashboard values and metadata.</param>
    void Initialize(Device device);

    /// <summary>
    /// Read in a module and populate the object.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="module">Source Netatmo module containing dashboard values and metadata.</param>
    void Initialize(Device device, StationModule module);

    /// <summary>
    /// Sets the dashboard data in Fields
    /// </summary>
    /// <param name="dashboardData"></param>
    void SetFields(Dashboard dashboardData);

    /// <summary>
    /// Set the tags for a device
    /// </summary>
    /// <param name="device"></param>
    void SetTags(Device device);

    /// <summary>
    /// Sets the tags for a module using the device as well for connect it to a home
    /// </summary>
    /// <param name="device"></param>
    /// <param name="module"></param>
    void SetTags(Device device, StationModule module);
}