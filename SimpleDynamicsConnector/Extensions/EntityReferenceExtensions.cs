using GuedesPlace.SimpleDynamicsConnector.Models;
namespace GuedesPlace.SimpleDynamicsConnector.Extensions;

public static class EntityReferenceExtension {
    public static string BuildODataIdStamp(this EntityReference entityReference, SimpleDynamicsConnector simpleDynamicsConnector)
    {
        return "{'@odata.id':'" + simpleDynamicsConnector.BuildPluralNameForEntity(entityReference.LogicalName) + "(" + entityReference.Id + ")'}";
    }
    public static string BuildODataBindReference(this EntityReference entityReference, SimpleDynamicsConnector simpleDynamicsConnector)
    {
        return $"{simpleDynamicsConnector.BuildPluralNameForEntity(entityReference.LogicalName)}({entityReference.Id})";
    }
}