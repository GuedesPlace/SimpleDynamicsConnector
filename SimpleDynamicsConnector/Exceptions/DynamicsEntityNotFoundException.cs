using System.Net;

namespace GuedesPlace.SimpleDynamicsConnector.Exceptions;

/// <summary>
/// Exception thrown when a Dynamics/Dataverse entity is not found (404).
/// </summary>
public class DynamicsEntityNotFoundException(
    string httpMethod,
    string path,
    string entityName,
    Guid? entityId = null,
    string? errorContent = null) : DynamicsConnectorException(
        httpMethod, 
        path, 
        HttpStatusCode.NotFound, 
        "Not Found", 
        errorContent, 
        null,
        $"Entity '{entityName}'{(entityId.HasValue ? $" with ID '{entityId}'" : "")} not found")
{
    /// <summary>
    /// Name of the entity that was not found
    /// </summary>
    public string EntityName { get; } = entityName;

    /// <summary>
    /// ID of the entity that was not found (if applicable)
    /// </summary>
    public Guid? EntityId { get; } = entityId;
}
