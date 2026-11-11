using Microsoft.Identity.Client;
using GuedesPlace.SimpleDynamicsConnector.Abstractions;

namespace GuedesPlace.SimpleDynamicsConnector.Authentication;

/// <summary>
/// Token provider for app-only authentication using client credentials (secret or certificate).
/// Wraps <see cref="IConfidentialClientApplication"/> and acquires tokens via
/// <c>AcquireTokenForClient</c> with built-in retry on transient network failures.
/// </summary>
public class ClientCredentialsTokenProvider : IDynamicsTokenProvider
{
    private readonly IConfidentialClientApplication _clientApp;

    public ClientCredentialsTokenProvider(IConfidentialClientApplication clientApp)
    {
        _clientApp = clientApp;
    }

    /// <inheritdoc/>
    public async Task<string> GetAccessTokenAsync(string resource, CancellationToken cancellationToken = default)
    {
        int maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var result = await _clientApp
                    .AcquireTokenForClient([$"{resource}/.default"])
                    .ExecuteAsync(cancellationToken);
                return result.AccessToken;
            }
            catch (HttpRequestException) when (i < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, i) * 100), cancellationToken);
            }
        }
        throw new HttpRequestException("Failed to acquire token after multiple retries.");
    }
}
