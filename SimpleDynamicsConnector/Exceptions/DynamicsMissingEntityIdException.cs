using System.Net;

namespace GuedesPlace.SimpleDynamicsConnector.Exceptions;

/// <summary>
/// Exception thrown when a create operation succeeds but the OData-EntityId header is missing from the response.
/// </summary>
public class DynamicsMissingEntityIdException(
    string path,
    string? requestPayload = null) : DynamicsConnectorException(
        "POST", 
        path, 
        HttpStatusCode.OK, 
        null, 
        null, 
        requestPayload,
        "Create operation succeeded but OData-EntityId header was not found in response")
{
}
