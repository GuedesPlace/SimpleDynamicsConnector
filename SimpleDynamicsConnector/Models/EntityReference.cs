namespace GuedesPlace.SimpleDynamicsConnector.Models;

public sealed class EntityReference : IEquatable<EntityReference>
{
    public Guid Id { set; get; }
    public string LogicalName { set; get; } = string.Empty;
    public string? Name { set; get; }

    public static EntityReference Create(Guid id, string logicalName, string? name = null)
    {
        return new EntityReference { Id = id, LogicalName = logicalName, Name = name };
    }
    // IEquatable<EntityReference> implementation
    public bool Equals(EntityReference? other)
    {
        if (other == null)
        {
            return false;
        }

        return Id == other.Id && LogicalName == other.LogicalName;
    }

    // Override of default Object.Equals()
    public override bool Equals(object? obj)
    {
        return Equals(obj as EntityReference);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, LogicalName);
    }
}