namespace GuedesPlace.SimpleDynamicsConnector.Models;

public class DynamicsConnectionConfiguration
{
    required public string CrmUrl { set; get; }
    required public string TenantId { set; get; }
    required public string ApplicationId { set; get; }

    /// <summary>
    /// Client secret for <see cref="DynamicsAuthenticationMode.ClientCredentials"/>.
    /// Not required for <see cref="DynamicsAuthenticationMode.Interactive"/> or
    /// <see cref="DynamicsAuthenticationMode.DeviceCode"/>.
    /// </summary>
    public string? ApplicationSecret { set; get; }

    /// <summary>
    /// Authentication flow used to acquire tokens. Defaults to
    /// <see cref="DynamicsAuthenticationMode.ClientCredentials"/>.
    /// </summary>
    public DynamicsAuthenticationMode AuthenticationMode { set; get; } = DynamicsAuthenticationMode.ClientCredentials;

    public Dictionary<string, string> CustomTablePluralMapping { set; get; } = new Dictionary<string, string>();
}