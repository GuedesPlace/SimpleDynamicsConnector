namespace GuedesPlace.SimpleDynamicsConnector.Models;

/// <summary>
/// Specifies how the SimpleDynamicsConnector acquires tokens for Dynamics/Dataverse.
/// </summary>
public enum DynamicsAuthenticationMode
{
    /// <summary>
    /// App-only authentication using a client secret or certificate.
    /// Requires <see cref="DynamicsConnectionConfiguration.ApplicationSecret"/>.
    /// Uses <see cref="Microsoft.Identity.Client.IConfidentialClientApplication"/>.
    /// </summary>
    ClientCredentials,

    /// <summary>
    /// Delegated user authentication via a system browser popup.
    /// Suitable for desktop CLIs with UI access.
    /// Uses <see cref="Microsoft.Identity.Client.IPublicClientApplication"/>.
    /// Requires the Entra ID app registration to be configured as a Public Client
    /// with a <c>http://localhost</c> redirect URI.
    /// </summary>
    Interactive,

    /// <summary>
    /// Delegated user authentication via the device code flow.
    /// Suitable for headless CLIs, SSH sessions, or environments without a browser.
    /// Uses <see cref="Microsoft.Identity.Client.IPublicClientApplication"/>.
    /// Requires the Entra ID app registration to have <c>Allow public client flows</c> enabled.
    /// </summary>
    DeviceCode
}
