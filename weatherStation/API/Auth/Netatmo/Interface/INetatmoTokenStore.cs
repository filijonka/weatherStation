using API.Auth.Netatmo.Services;

using System.Threading.Tasks;
using System.Threading;
namespace API.Auth.Netatmo.Interface;

/// <summary>
/// Abstraction for persisting Netatmo tokens.
/// </summary>
public interface INetatmoTokenStore
{
    /// <summary>
    /// Persists token information.
    /// </summary>
    /// <param name="tokenInfo">Token information to store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task SaveAsync(NetatmoTokenInfo tokenInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Loads token information if available.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Token information.</returns>
    Task<NetatmoTokenInfo> LoadAsync(CancellationToken cancellationToken);
}
