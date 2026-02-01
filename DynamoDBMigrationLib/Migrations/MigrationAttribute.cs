namespace DynamoDBMigrationLib.Migrations;

[AttributeUsage(AttributeTargets.Class)]
public sealed class MigrationAttribute : Attribute
{
    public MigrationAttribute(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty("id");
        Id = id;
    }

    public string Id { get; }
}
