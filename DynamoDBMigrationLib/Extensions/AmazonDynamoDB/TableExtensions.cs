using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace DynamoDBMigrationLib.Extensions.AmazonDynamoDB;

internal static class TableExtensions
{
    internal static async Task WaitTillTableCreatedAsync(
        this IAmazonDynamoDB client,
        CreateTableResponse response,
        CancellationToken cancellationToken = default
    ) => await client.WaitTableStatus(
        response.TableDescription,
        status => status != Constants.Constants.TABLE_STATUS_ACTIVE,
        cancellationToken
    );

    internal static async Task WaitTillTableDeleted(
        this IAmazonDynamoDB client,
        DeleteTableResponse response,
        CancellationToken cancellationToken = default
    ) => await client.WaitTableStatus(
        response.TableDescription,
        status => status == Constants.Constants.TABLE_STATUS_DELETING,
        cancellationToken
    );

    private static async Task WaitTableStatus(
        this IAmazonDynamoDB client,
        TableDescription tableDescription,
        Func<string, bool> expectedStatus,
        CancellationToken cancellationToken = default
    )
    {
        var status = tableDescription.TableStatus;
        try
        {
            while (expectedStatus(status.Value))
            {
                Thread.Sleep(1000);

                var res = await client.DescribeTableAsync(new DescribeTableRequest
                {
                    TableName = tableDescription.TableName
                },
                cancellationToken);

                status = res.Table.TableStatus;
            }
        }
        catch (ResourceNotFoundException)
        { /* Ignore exception if table not found */ }
    }
}
