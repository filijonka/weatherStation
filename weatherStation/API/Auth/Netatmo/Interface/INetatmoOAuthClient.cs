using API.Auth.Netatmo.Services;

using System.Threading.Tasks;
using System.Threading;
namespace API.Auth.Netatmo.Interface;

/// <summary>
/// OAuth client abstraction for Netatmo authorization.
/// </summary>
public interface INetatmoOAuthClient
{
    /// <summary>
    /// Builds the Netatmo authorization URL.
    /// </summary>
    /// <param name="state">Opaque state value for CSRF protection.</param>
    /// <returns>Authorization URL.</returns>
    string BuildAuthorizeUrl(string state);

    /// <summary>
    /// Exchanges an authorization code for tokens.
    /// </summary>
    /// <param name="code">Authorization code returned by Netatmo.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Token information.</returns>
    Task<NetatmoTokenInfo> ExchangeCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes access tokens using a refresh token.
    /// </summary>
    /// <param name="refreshToken">Refresh token.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Token information.</returns>
    Task<NetatmoTokenInfo> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
}
