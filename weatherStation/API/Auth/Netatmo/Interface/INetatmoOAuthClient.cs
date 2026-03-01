using API.Auth.Netatmo.Services;

using System.Threading.Tasks;
using System.Threading;
using API.Responses;

namespace API.Auth.Netatmo.Interface;

/// <summary>
/// OAuth client abstraction for Netatmo authorization.
/// </summary>
public interface INetatmoOAuthClient
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<NetatmoAuthStatusResponse> Login(CancellationToken cancellationToken);

    /// <summary>
    /// Exchanges an authorization code for tokens.
    /// </summary>
    /// <param name="code">Authorization code returned by Netatmo.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Token information.</returns>
    Task<NetatmoTokenInfo> ExchangeCodeAsync(string code, CancellationToken cancellationToken);
}
