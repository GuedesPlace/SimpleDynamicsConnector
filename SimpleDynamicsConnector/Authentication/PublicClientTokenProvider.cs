using Microsoft.Identity.Client;
using GuedesPlace.SimpleDynamicsConnector.Abstractions;
using GuedesPlace.SimpleDynamicsConnector.Models;

namespace GuedesPlace.SimpleDynamicsConnector.Authentication;

/// <summary>
/// Token provider for delegated user authentication using either an interactive browser
/// login or the device code flow. Wraps <see cref="IPublicClientApplication"/> and
/// uses the MSAL token cache to minimise interactive prompts.
/// </summary>
public class PublicClientTokenProvider : IDynamicsTokenProvider
{
    private readonly IPublicClientApplication _clientApp;
    private readonly DynamicsAuthenticationMode _mode;
    private readonly Func<DeviceCodeResult, Task>? _deviceCodeCallback;

    /// <param name="clientApp">The MSAL public client application.</param>
    /// <param name="mode">Must be <see cref="DynamicsAuthenticationMode.Interactive"/> or <see cref="DynamicsAuthenticationMode.DeviceCode"/>.</param>
    /// <param name="deviceCodeCallback">
    /// Optional custom handler for the device code message.
    /// Defaults to <see cref="Console.WriteLine"/> when <see langword="null"/>.
    /// Only used when <paramref name="mode"/> is <see cref="DynamicsAuthenticationMode.DeviceCode"/>.
    /// </param>
    public PublicClientTokenProvider(
        IPublicClientApplication clientApp,
        DynamicsAuthenticationMode mode,
        Func<DeviceCodeResult, Task>? deviceCodeCallback = null)
    {
        if (mode == DynamicsAuthenticationMode.ClientCredentials)
            throw new ArgumentException(
                $"{nameof(PublicClientTokenProvider)} does not support {DynamicsAuthenticationMode.ClientCredentials}. Use {nameof(ClientCredentialsTokenProvider)} instead.",
                nameof(mode));

        _clientApp = clientApp;
        _mode = mode;
        _deviceCodeCallback = deviceCodeCallback;
    }

    /// <inheritdoc/>
    public async Task<string> GetAccessTokenAsync(string resource, CancellationToken cancellationToken = default)
    {
        string[] scopes = [$"{resource}/.default"];

        // Attempt silent auth first to avoid re-prompting on every request
        var accounts = await _clientApp.GetAccountsAsync();
        if (accounts.Any())
        {
            try
            {
                var silentResult = await _clientApp
                    .AcquireTokenSilent(scopes, accounts.First())
                    .ExecuteAsync(cancellationToken);
                return silentResult.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                // Token expired or not in cache – fall through to interactive flow
            }
        }

        return _mode switch
        {
            DynamicsAuthenticationMode.Interactive => await AcquireInteractiveAsync(scopes, cancellationToken),
            DynamicsAuthenticationMode.DeviceCode   => await AcquireDeviceCodeAsync(scopes, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported authentication mode: {_mode}.")
        };
    }

    private async Task<string> AcquireInteractiveAsync(string[] scopes, CancellationToken cancellationToken)
    {
        var result = await _clientApp
            .AcquireTokenInteractive(scopes)
            .ExecuteAsync(cancellationToken);
        return result.AccessToken;
    }

    private async Task<string> AcquireDeviceCodeAsync(string[] scopes, CancellationToken cancellationToken)
    {
        var callback = _deviceCodeCallback ?? (deviceCodeResult =>
        {
            Console.WriteLine(deviceCodeResult.Message);
            return Task.CompletedTask;
        });

        var result = await _clientApp
            .AcquireTokenWithDeviceCode(scopes, callback)
            .ExecuteAsync(cancellationToken);
        return result.AccessToken;
    }
}
