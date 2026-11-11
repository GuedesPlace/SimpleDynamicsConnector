namespace GuedesPlace.SimpleDynamicsConnector.Abstractions;

/// <summary>
/// Abstracts Bearer token acquisition for Dynamics/Dataverse, decoupling the connector
/// from any specific authentication flow (client credentials, interactive, device code).
/// </summary>
public interface IDynamicsTokenProvider
{
    /// <summary>
    /// Returns a valid Bearer access token for the given Dynamics/Dataverse resource URL.
    /// </summary>
    /// <param name="resource">The CRM base URL (e.g. <c>https://contoso.crm.dynamics.com</c>). The scope <c>{resource}/.default</c> is derived from this.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> GetAccessTokenAsync(string resource, CancellationToken cancellationToken = default);
}
