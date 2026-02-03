using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace DynamoDBMigrationLib.Migrations;

public interface IMigrationOperation 
{
    Task Execute(IAmazonDynamoDB client, CancellationToken cancellationToken);
}

internal interface IClientOperation<in T> : IMigrationOperation where T : class
{
    Task Execute(IAmazonDynamoDB client, T request, CancellationToken cancellationToken);
}

internal interface IContextOperation<in T> : IMigrationOperation where T : class
{
    Task Execute(IAmazonDynamoDB client, IEnumerable<T> items, CancellationToken cancellationToken);
}

internal sealed class ClientOperation<T>(
    T request,
    Func<IAmazonDynamoDB, T, CancellationToken, Task> operation
) : IClientOperation<T> where T : class
{
    private readonly T request = request;
    private readonly Func<IAmazonDynamoDB, T, CancellationToken, Task> _instruction = operation;

    public async Task Execute(IAmazonDynamoDB client, CancellationToken cancellationToken)
        => await Execute(client, request, cancellationToken);

    public async Task Execute(IAmazonDynamoDB client, T request, CancellationToken cancellationToken)
        => await _instruction(client, request, cancellationToken);
}

internal sealed class ContextOperation<T>(
    IEnumerable<T> items,
    Func<IDynamoDBContext, IEnumerable<T>, CancellationToken, Task> operation
) : IContextOperation<T> where T : class
{
    private readonly IEnumerable<T> _items = items;
    private readonly Func<IDynamoDBContext, IEnumerable<T>, CancellationToken, Task> _instruction = operation;

    public async Task Execute(IAmazonDynamoDB client, CancellationToken cancellationToken)
        => await Execute(client, _items, cancellationToken);

    public async Task Execute(IAmazonDynamoDB client, IEnumerable<T> items, CancellationToken cancellationToken)
    {
        var context = new DynamoDBContextBuilder()
            .WithDynamoDBClient(new Func<IAmazonDynamoDB>(() => client))
            .Build();

        await _instruction(context, items, cancellationToken);
    }
}
