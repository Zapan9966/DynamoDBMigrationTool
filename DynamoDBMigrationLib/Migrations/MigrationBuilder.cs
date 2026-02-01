using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using DynamoDBMigrationLib.Extensions.AmazonDynamoDB;

namespace DynamoDBMigrationLib.Migrations;

public sealed class MigrationBuilder : List<IMigrationOperation>
{
    public void CreateTable(CreateTableRequest createTableRequest)
        => Add(new ClientOperation<CreateTableRequest>(
            createTableRequest, async (client, request, ct) =>
            {
                var response = await client.CreateTableAsync(request, ct);
                await client.WaitTillTableCreatedAsync(response, ct);
            }
        ));

    public void DeleteTable(DeleteTableRequest deleteTableRequest)
        => Add(new ClientOperation<DeleteTableRequest>(
            deleteTableRequest, async (client, request, ct) =>
            {
                var response = await client.DeleteTableAsync(request, ct);
                await client.WaitTillTableDeleted(response, ct);
            }
        ));

    public void BatchWriteItem(BatchWriteItemRequest batchWriteItemRequest)
        => Add(new ClientOperation<BatchWriteItemRequest>(
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
        => Add(new ContextOperation<T>(
            items, async (context, items, ct) =>
            {
                var batchWrite = context.CreateBatchWrite<T>();
                batchWrite.AddPutItems(items);
                await batchWrite.ExecuteAsync(ct);
            }
        ));

    public void DeleteItems<T>(IEnumerable<T> items) where T : class
        => Add(new ContextOperation<T>(
            items, async (context, items, ct) =>
            {
                var batchWrite = context.CreateBatchWrite<T>();
                batchWrite.AddDeleteItems(items);
                await batchWrite.ExecuteAsync(ct);
            }
        ));

    public void Query<T>(Func<T, CancellationToken, Task> clientAction)
        => Add(new ClientOperation<Func<T, CancellationToken, Task>>(
            clientAction, async (client, action, ct) =>
            {
                if (typeof(T) != typeof(IAmazonDynamoDB) && typeof(T) != typeof(IDynamoDBContext))
                {
                    throw new NotSupportedException($"Query generic type '{typeof(IAmazonDynamoDB).Name}' is naot a valid type");
                }

                object dynamoDb = typeof(T) == typeof(IAmazonDynamoDB)
                    ? client
                    : new DynamoDBContextBuilder()
                        .WithDynamoDBClient(new Func<IAmazonDynamoDB>(() => client))
                        .Build();

                await clientAction((T)dynamoDb, ct);
            }
        ));
}
