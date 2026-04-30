using System.Net;

namespace GuedesPlace.SimpleDynamicsConnector.Exceptions;

/// <summary>
/// Exception thrown when authentication or authorization fails (401 Unauthorized or 403 Forbidden).
/// </summary>
public class DynamicsAuthenticationException(
    string httpMethod,
    string path,
    HttpStatusCode statusCode,
    string? errorContent = null) : DynamicsConnectorException(
        httpMethod, 
        path, 
        statusCode,
        statusCode == HttpStatusCode.Unauthorized ? "Unauthorized" : "Forbidden",
        errorContent, 
        null,
        statusCode == HttpStatusCode.Unauthorized 
                ? "Authentication failed. Check credentials and token validity" 
                : "Access forbidden. Insufficient permissions")
{
}
