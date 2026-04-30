using System.Net;
using System.Text;

namespace GuedesPlace.SimpleDynamicsConnector.Exceptions;

/// <summary>
/// Base exception class for all Dynamics/Dataverse connector errors.
/// Provides structured error information from HTTP responses.
/// </summary>
public class DynamicsConnectorException(
    string httpMethod,
    string path,
    HttpStatusCode statusCode,
    string? reasonPhrase = null,
    string? errorContent = null,
    string? requestPayload = null,
    string? additionalMessage = null) : Exception(BuildMessage(httpMethod, path, statusCode, reasonPhrase, errorContent, requestPayload, additionalMessage))
{
    /// <summary>
    /// HTTP method used in the request (GET, POST, PATCH, DELETE, etc.)
    /// </summary>
    public string HttpMethod { get; } = httpMethod;

    /// <summary>
    /// API path that was called
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// HTTP status code returned by the server
    /// </summary>
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>
    /// Reason phrase from the HTTP response
    /// </summary>
    public string? ReasonPhrase { get; } = reasonPhrase;

    /// <summary>
    /// Error content returned by the Dynamics/Dataverse API
    /// </summary>
    public string? ErrorContent { get; } = errorContent;

    /// <summary>
    /// Request payload that was sent (if applicable)
    /// </summary>
    public string? RequestPayload { get; } = requestPayload;

    private static string BuildMessage(
        string httpMethod,
        string path,
        HttpStatusCode statusCode,
        string? reasonPhrase,
        string? errorContent,
        string? requestPayload,
        string? additionalMessage)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(additionalMessage))
            sb.Append($"{additionalMessage} - ");

        sb.Append($"Dynamics API error during {httpMethod} request to '{path}'. ");
        sb.Append($"Status: {(int)statusCode} {statusCode}");

        if (!string.IsNullOrWhiteSpace(reasonPhrase))
            sb.Append($" - {reasonPhrase}");

        if(!string.IsNullOrWhiteSpace(requestPayload))
            sb.Append($". Request payload: {requestPayload}");

        if (!string.IsNullOrWhiteSpace(errorContent))
            sb.Append($". Server response: {errorContent}");

        return sb.ToString();
    }
}
