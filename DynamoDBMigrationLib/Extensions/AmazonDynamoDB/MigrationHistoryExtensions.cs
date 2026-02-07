using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;

namespace DynamoDBMigrationLib.Extensions.AmazonDynamoDB;

internal static class MigrationHistoryExtensions
{
    internal static async Task CreateMigrationHistoryAsync(
        this IAmazonDynamoDB client,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var tableName = Constants.Constants.MIGRATION_HISTORY_TABLE;
            var response = await client.CreateTableAsync(new CreateTableRequest
            {
                TableName = tableName,
                AttributeDefinitions = [
                    new("type", "S"),
                new("name", "S"),
            ],
                KeySchema = [
                    new("type", "HASH"),
                new("name", "RANGE"),
            ],
                ProvisionedThroughput = new(5, 5)
            }, cancellationToken);
            await client.WaitTillTableCreatedAsync(response, cancellationToken);
        }
        catch (ResourceInUseException) 
        { /* Ignore exception if table already exists */ }
    }

    internal static async Task<List<string>> GetAppliedMigrationAsync(
        this IAmazonDynamoDB client,
        CancellationToken cancellationToken = default
    )
    {
        var request = new QueryRequest
        {
            TableName = Constants.Constants.MIGRATION_HISTORY_TABLE,
            KeyConditions = new Dictionary<string, Condition>
            {
                {
                    "type",
                    new Condition
                    {
                        ComparisonOperator = ComparisonOperator.EQ,
                        AttributeValueList = [ new("migration") ]
                    }
                }
            }
        };
        var response = await client.QueryAsync(request, cancellationToken);

        var items = response.Items
            .Select(i => i.TryGetValue("name", out AttributeValue? value) ? value?.S : null)
            .OfType<string>();

        return [.. items];
    }

    internal static async Task AddMigrationHistory(
        this IAmazonDynamoDB client,
        string migrationId,
        CancellationToken cancellationToken = default
    )
    {
        var request = new PutItemRequest
        {
            TableName = Constants.Constants.MIGRATION_HISTORY_TABLE,
            Item = new()
            {
                { "type", new("migration") },
                { "name", new(migrationId) },
            }
        };
        await client.PutItemAsync(request, cancellationToken);
    }

    internal static async Task DeleteMigrationHistory(
        this IAmazonDynamoDB client,
        string migrationId,
        CancellationToken cancellationToken = default
    )
    {
        var request = new DeleteItemRequest
        {
            TableName = Constants.Constants.MIGRATION_HISTORY_TABLE,
            Key = new()
            {
                { "type", new("migration") },
                { "name", new(migrationId) },
            }
        };
        await client.DeleteItemAsync(request, cancellationToken);
    }

}
