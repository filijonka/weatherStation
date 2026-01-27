using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace API.Interface.Logic;

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
    /// <returns>Raw JSON response as JsonElement.</returns>
    Task<JsonElement> GetHomesDataAsync(string accessToken, string[] gatewayTypes, CancellationToken cancellationToken);
}
