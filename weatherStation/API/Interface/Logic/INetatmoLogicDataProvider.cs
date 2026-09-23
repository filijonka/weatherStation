using Application.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace API.Interface.Logic;

/// <summary>
/// Room information mapped to a module.
/// </summary>
/// <param name="RoomId">Room id associated with the module.</param>
/// <param name="RoomName">Room name associated with the module.</param>
public sealed record ModuleRoomInfo(string RoomId, string RoomName);

/// <summary>
/// Data provider abstraction for fetching data from Netatmo API.
/// </summary>
public interface INetatmoLogicDataProvider
{
    /// <summary>
    /// Fetches home data from Netatmo API.
    /// </summary>
    /// <param name="accessToken">Netatmo access token.</param>
    /// <param name="gatewayTypes">Optional gateway types filter (NLG, OTH, NBG, BNMH, BNS).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Deserialized Netatmo homesdata response.</returns>
    Task<JsonHome> GetHomesDataAsync(string accessToken, string[] gatewayTypes, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="homes"></param>
    /// <returns></returns>
    IReadOnlyDictionary<string, ModuleRoomInfo> GetModuleRoomMap(JsonHome homes);
    
    /// <summary>
    /// Fetches station/module data from Netatmo API (/getstationsdata).
    /// </summary>
    /// <param name="accessToken">Netatmo access token.</param>
    /// <param name="moduleId">Module/device id (MAC-like format).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Deserialized Netatmo getstationsdata response.</returns>
    Task<JsonStationData> GetModuleDataAsync(string accessToken, string moduleId, CancellationToken cancellationToken);
}
