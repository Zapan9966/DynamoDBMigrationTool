using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using DynamoDBMigrationLib.Extensions.AmazonDynamoDB;
using DynamoDBMigrationLib.Migrations.Interfaces;

namespace DynamoDBMigrationLib.Migrations;

public sealed class MigrationBuilder : List<IMigrationOperation>
{
    public void CreateTable(CreateTableRequest createTableRequest)
        => Add(new MigrationOperation<IAmazonDynamoDB, CreateTableRequest>(
            createTableRequest, async (client, request, ct) =>
            {
                var response = await client.CreateTableAsync(request, ct);
                await client.WaitTillTableCreatedAsync(response, ct);
            }
        ));

    public void DeleteTable(DeleteTableRequest deleteTableRequest)
        => Add(new MigrationOperation<IAmazonDynamoDB, DeleteTableRequest>(
            deleteTableRequest, async (client, request, ct) =>
            {
                var response = await client.DeleteTableAsync(request, ct);
                await client.WaitTillTableDeleted(response, ct);
            }
        ));

    public void BatchWriteItem(BatchWriteItemRequest batchWriteItemRequest)
        => Add(new MigrationOperation<IAmazonDynamoDB, BatchWriteItemRequest>(
            batchWriteItemRequest, async (client, request, ct) =>
            {
                BatchWriteItemResponse response;
                do
                {
                    response = await client.BatchWriteItemAsync(request, ct);
                    var unprocessed = response.UnprocessedItems;
                    request.RequestItems = unprocessed;
                }
                while (response.UnprocessedItems.Count > 0);
            }
        ));

    public void PutItems<T>(IEnumerable<T> items) where T : class
        => Add(new MigrationOperation<IDynamoDBContext, IEnumerable<T>>(
            items, async (context, items, ct) =>
            {
                var batchWrite = context.CreateBatchWrite<T>();
                batchWrite.AddPutItems(items);
                await batchWrite.ExecuteAsync(ct);
            }
        ));

    public void DeleteItems<T>(IEnumerable<T> items) where T : class
        => Add(new MigrationOperation<IDynamoDBContext, IEnumerable<T>>(
            items, async (context, items, ct) =>
            {
                var batchWrite = context.CreateBatchWrite<T>();
                batchWrite.AddDeleteItems(items);
                await batchWrite.ExecuteAsync(ct);
            }
        ));

    public void Query<T>(Func<T, CancellationToken, Task> clientAction)
        => Add(new MigrationOperation<T, Func<T, CancellationToken, Task>>(
            clientAction, async (dynamoDb, action, ct) =>
            {
                if (typeof(T) != typeof(IAmazonDynamoDB) && typeof(T) != typeof(IDynamoDBContext))
                {
                    throw new NotSupportedException($"Query generic type '{typeof(IAmazonDynamoDB).Name}' is not a valid type");
                }
                await clientAction(dynamoDb, ct);
            }
        ));
}
