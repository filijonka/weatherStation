using API.Interface.Logic;
using API.Interface.Services;
using Application.Models;
using Persistence.Influx.Interface;

using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Persistence.Models;
using Persistence.Models.Interface;
using System;

namespace API.Services;

/// <inheritdoc />
public sealed class NetatmoService : INetatmoService
{
    private readonly IInfluxWriteService influxWriteService;
    private readonly INetatmoLogicDataProvider netatmoLogicDataProvider;
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetatmoService"/> class.
    /// </summary>
    /// <param name="influxWriteService">Influx write service.</param>
    /// <param name="netatmoLogicDataProvider"></param>
    /// <param name="logger">Serilog logger.</param>
    public NetatmoService(
        IInfluxWriteService influxWriteService,
        INetatmoLogicDataProvider netatmoLogicDataProvider,
        ILogger logger
    )
    {
        this.influxWriteService = influxWriteService;
        this.netatmoLogicDataProvider = netatmoLogicDataProvider;
        this.logger = logger.ForContext<NetatmoService>();
    }

    /// <inheritdoc />
    public async Task<int> FetchAndStoreAsync(string token, CancellationToken cancellationToken)
    {
        JsonHome homes = await netatmoLogicDataProvider.GetHomesDataAsync(
            token,
            gatewayTypes: [],
            cancellationToken
        ); 
        IReadOnlyDictionary<string, ModuleRoomInfo> moduleRoomLookup = this.netatmoLogicDataProvider.GetModuleRoomMap(homes);

        if (moduleRoomLookup.Count == 0)
        {
            throw new InvalidOperationException("No module to room mappings available from homes data.");
        }

        string moduleId = string.Empty;
        foreach (KeyValuePair<string, ModuleRoomInfo> entry in moduleRoomLookup)
        {
            moduleId = entry.Key;
            break;
        }

        if (string.IsNullOrWhiteSpace(moduleId))
        {
            throw new InvalidOperationException("No module id available for stations data request.");
        }

        JsonStationData stationData = await this.netatmoLogicDataProvider.GetModuleDataAsync(
            token,
            moduleId,
            cancellationToken
        );

        if (!stationData.IsValid())
        {
            return 0;
        }

        NetatmoPointFactory factory = new NetatmoPointFactory("netatmo");
        List<IInfluxPoint> points = new List<IInfluxPoint>();
        Device device = stationData.Body.Devices[0];
        IInfluxPoint point = factory.Create(device.Type); 
        point.Initialize(device);
        points.Add(point);

        if (device.Modules.Count == 0)
        {
            return points.Count;
        }

        foreach (StationModule module in device.Modules)
        {
            point = factory.Create(module.Type);

            module.RoomId = moduleRoomLookup.TryGetValue(module.Id, out ModuleRoomInfo moduleRoomInfo)
                ? moduleRoomInfo.RoomId
                : string.Empty;
            point.Initialize(module);

            points.Add(point);
        }

        int written = await this.influxWriteService.WriteAsync(points, cancellationToken);   
        
        return written;
    }
}
