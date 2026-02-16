using API.Auth.Netatmo.Interface;
using API.Auth.Netatmo.Services;
using API.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.Helpers;

/// <summary>
/// Helper class for Netatmo authentication status checks.
/// </summary>
public static class NetatmoAuthHelper
{
    /// <summary>
    /// Gets the Netatmo authentication status.
    /// </summary>
    /// <param name="tokenStore">Token store.</param>
    /// <param name="baseUrl">Base URL for building login link (e.g., https://localhost:8080).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Authentication status response.</returns>
    public static async Task<NetatmoAuthStatusResponse> GetAuthStatusAsync(
        INetatmoTokenStore tokenStore,
        string baseUrl,
        CancellationToken cancellationToken
    )
    {
        NetatmoTokenInfo tokenInfo = await tokenStore.LoadAsync(cancellationToken);
        bool authenticated = tokenInfo != null && tokenInfo.ExpiresAtUtc > DateTime.UtcNow;
        string status = authenticated ? "Connected" : "Login required";
        string loginUrl = authenticated ? string.Empty : $"{baseUrl}/api/v1/netatmo/auth/login";
        DateTime? expiresAtUtc = authenticated ? tokenInfo!.ExpiresAtUtc : null;
        
        int statusCode = authenticated ? 200 : 401;
        string message = authenticated ? string.Empty : "Netatmo authentication required. Please log in first.";

        return new NetatmoAuthStatusResponse
        {
            Authenticated = authenticated,
            Token = authenticated ? tokenInfo.AccessToken : "",
            Status = status,
            LoginUrl = loginUrl,
            ExpiresAtUtc = expiresAtUtc,
            StatusCode = statusCode,
            Message = message
        };
    }
}
