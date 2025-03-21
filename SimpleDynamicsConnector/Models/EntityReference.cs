namespace GuedesPlace.SimpleDynamicsConnector.Models;

public class EntityReference : IEquatable<EntityReference>
{
    public Guid Id { set; get; }
    public string LogicalName { set; get; } = string.Empty;
    public string? Name { set; get; }

    public static EntityReference Create(Guid id, string logicalName, string? name = null)
    {
        return new EntityReference { Id = id, LogicalName = logicalName, Name = name };
    }
    // IEquatable<MyClass> implementation
    public bool Equals(EntityReference? other)
    {
        if (other == null)
        {
            return false;
        }

        return Id == other.Id;
    }

    // Override of default Object.Equals()
    public override bool Equals(object? other)
    {
        return Equals(other as EntityReference);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}