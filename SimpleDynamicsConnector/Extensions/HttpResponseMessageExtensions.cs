using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using GuedesPlace.SimpleDynamicsConnector.Exceptions;

namespace GuedesPlace.SimpleDynamicsConnector.Extensions;

/// <summary>
/// Extension methods for HttpResponseMessage to convert failed responses into Dynamics-specific exceptions.
/// </summary>
public static class HttpResponseMessageExtensions
{
    /// <summary>
    /// Converts an HTTP response into a specialized DynamicsConnectorException based on the status code.
    /// </summary>
    /// <param name="response">The HTTP response message</param>
    /// <param name="httpMethod">HTTP method used (GET, POST, PATCH, DELETE)</param>
    /// <param name="path">API path that was called</param>
    /// <param name="payload">Request payload (if applicable)</param>
    /// <param name="additionalMessage">Optional additional context message</param>
    /// <returns>A specialized exception based on the response status code</returns>
    public static async Task<DynamicsConnectorException> ToDynamicsExceptionAsync(
        this HttpResponseMessage response,
        string httpMethod,
        string path,
        string? payload = null,
        string? additionalMessage = null)
    {
        string errorContent = await response.Content.ReadAsStringAsync();

        // Return specialized exceptions based on status code
        return response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => new DynamicsRateLimitException(
                httpMethod,
                path,
                response.Headers.RetryAfter?.Delta,
                errorContent,
                payload),

            HttpStatusCode.NotFound => new DynamicsEntityNotFoundException(
                httpMethod,
                path,
                ExtractEntityNameFromPath(path),
                ExtractEntityIdFromPath(path),
                errorContent),

            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new DynamicsAuthenticationException(
                httpMethod,
                path,
                response.StatusCode,
                errorContent),

            _ => new DynamicsConnectorException(
                httpMethod,
                path,
                response.StatusCode,
                response.ReasonPhrase,
                errorContent,
                payload,
                additionalMessage)
        };
    }

    /// <summary>
    /// Extracts the entity name from a Dynamics API path.
    /// </summary>
    /// <param name="path">API path like "accounts(guid)" or "accounts"</param>
    /// <returns>The entity name (e.g., "accounts")</returns>
    private static string ExtractEntityNameFromPath(string path)
    {
        var parts = path.Split('(')[0].Split('/');
        return parts[^1];
    }

    /// <summary>
    /// Extracts the entity ID (GUID) from a Dynamics API path.
    /// </summary>
    /// <param name="path">API path like "accounts(guid)"</param>
    /// <returns>The entity GUID if found, otherwise null</returns>
    private static Guid? ExtractEntityIdFromPath(string path)
    {
        var match = Regex.Match(path, @"\(([a-f0-9-]+)\)");
        return match.Success && Guid.TryParse(match.Groups[1].Value, out var id) ? id : null;
    }

    /// <summary>
    /// Deserializes the response content to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="response">The HTTP response message</param>
    /// <returns>Deserialized object of type T</returns>
    public static async Task<T?> ToObjectAsync<T>(this HttpResponseMessage response)
    {
        string result = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(result);
    }
}
