using System.Net;

namespace GuedesPlace.SimpleDynamicsConnector.Exceptions;

/// <summary>
/// Exception thrown when Dynamics/Dataverse API returns 429 Too Many Requests (rate limiting).
/// </summary>
public class DynamicsRateLimitException : DynamicsConnectorException
{
    /// <summary>
    /// Time to wait before retrying, as specified by the server's RetryAfter header
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    public DynamicsRateLimitException(
        string httpMethod,
        string path,
        TimeSpan? retryAfter,
        string? errorContent = null,
        string? requestPayload = null)
        : base(
            httpMethod, 
            path, 
            HttpStatusCode.TooManyRequests, 
            "Too Many Requests", 
            errorContent, 
            requestPayload,
            retryAfter.HasValue 
                ? $"Rate limit exceeded. Retry after {retryAfter.Value.TotalSeconds:F0} seconds" 
                : "Rate limit exceeded")
    {
        RetryAfter = retryAfter;
    }
}
