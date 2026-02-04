# DynamoDB Migration Tool
DotNet tool to manage migrations on DynamoDB database.

By default, the tool work on the current directory.\
Appsettings files are parsed (based on environment) in order to find a configuration for DynamoDB.\
Migrations are stored as class in the target application (like EF Core do), the tool will **always** build the application 
and use the builded Assembly as if it were part of the tool.

## Update v0.0.5
 - Fix migration template
 - Rework to use DynamoDB services defined in the target application
 - Test with cloud instance
 - Add unit test project

## Disclaimer
This tool target ASP.NET Web application, it has not been tested with desktop application.

## Installation
```bash
dotnet tool install --global DynamoDBMigrationTool
```

## Application bootstrap

### Add DynamoDBMigrationLib
Add a reference to the *DynamoDBMigrationLib* library
```bash
dotnet add package DynamoDBMigrationLib
```

### Bootstrap class
Create a class that inherits `DynamoDBMigrationBootstrap(IConfiguration)`.\
Add your DynamoDB configuration inside the `ConfigureServices(IServiceCollection services)` implementation.
```csharp
public class MigrationToolBoostrap(IConfiguration configuration) 
    : DynamoDBMigrationBootstrap(configuration)
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<IAmazonDynamoDB>()
            .AddSingleton<IDynamoDBContext, DynamoDBContext>();
    }
}
```

### Register DynamoDBMigrationTool service
In `Program.cs`, add the following line
```csharp
builder.Services.AddDynamoDBMigrationTool(new MigrationToolBoostrap(builder.Configuration))
```

### (Optionnal) Apply migration on startup
At the end of `Program.cs`, add the following lines:
```csharp
using var scope = app.Services.CreateScope();
await scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateAsync();
```

## Command line usage
```bash
dynamodb [command] [options]
dynamodb [command] [options]
```
#### Options
| Option             | Description				| Example            |
| :----------------- | :----------------------- | :----------------- |
| --version          | Show version information | dynamodb --version |
| -? \| -h \| --help | Show help information    | dynamodb --help    |

#### Subcommands
| Command    | Example                                         |
| :--------- | :---------------------------------------------- | 
| migration  | dotnet dynamodb migration [command] \[options\] |

### Migration command
```bash
dynamodb migration [command] [options]
dynamodb migration [command] [options]
```
#### Options
| Option             | Description				| Example                   |
| :----------------- | :----------------------- | :------------------------ |
| -? \| -h \| --help | Show help information    | dynamodb migration --help |

#### Subcommands
| Command | Example                                                       |
| :------ | :------------------------------------------------------------ | 
| add     | dotnet dynamodb migration add \[options\] \<Migration name\>  |
| down    | dotnet dynamodb migration down \[options\] \<Migration name\> |
| up      | dotnet dynamodb migration up \[options\]                      |

### Add command
```bash
dynamodb migration add [options] <Migration name>
dynamodb migration add [options] <Migration name>
```
#### Arguments
| Argumanet      | Description                         | Example                            |
| :------------- | :---------------------------------- | :--------------------------------- | 
| Migration name | **Required**. Name of the Migration | dynamodb migration add MyMigration |


#### Options
| Option             | Description				| Example                       |
| :----------------- | :----------------------- | :---------------------------- |
| -o \| --output     | The output directory where migrations will be stored. Default value is: Migrations | dynamodb migration add MyMigration -o Data\Migration |
| -r \| --root       | Root directory of the application containing migrations  | dynamodb migration add MyMigration -r "/path/to/application" |
| -? \| -h \| --help | Show help information    | dynamodb migration add --help |

### Down command
```bash
dynamodb migration down [options] <Migration name>
dynamodb migration down [options] <Migration name>
```
#### Arguments
| Argument       | Description                         | Example                             |
| :------------- | :---------------------------------- | :---------------------------------- | 
| Migration name | **Optional**. Name of the Migration | dynamodb migration down MyMigration |

If you don't specify a migration name, only the last migration will be reverted.\
If you enter the name of a migration, all migrations will be reverted until the entered migration is reverted.

#### Options
| Option             | Description				| Example                               |
| :----------------- | :----------------------- | :------------------------------------ |
| -r \| --root       | Root directory of the application containing migrations   | dynamodb migration down -r "/path/to/application" |
| -? \| -h \| --help | Show help information    | dynamodb migration down --help |

### Up command
```bash
dynamodb migration up [options]
dynamodb migration up [options]
```
The `up` command will apply all migrations that have not yet been applied.

#### Options
| Option             | Description				| Example                               |
| :----------------- | :----------------------- | :------------------------------------ |
| -r \| --root       | Root directory of the application containing migrations   | dynamodb migration up -r "/path/to/application" |
| -? \| -h \| --help | Show help information    | dynamodb migration up --help   |

## Migration functions
You can do almost every operations inside migration files using the following low or high level functions.

### CreateTable(CreateTableRequest)
```csharp
migrationBuilder.CreateTable(new CreateTableRequest
{
    TableName = "MyTable",
    ProvisionedThroughput = new ProvisionedThroughput(10, 5),
    AttributeDefinitions = [
        new AttributeDefinition("id", "N"),
        new AttributeDefinition("sk", "S"),
    ],
    KeySchema = [
        new KeySchemaElement("id", "HASH"),
        new KeySchemaElement("sk", "RANGE"),
    ],
    GlobalSecondaryIndexes = [
        new GlobalSecondaryIndex
        {
            IndexName = "GSI1",
            ProvisionedThroughput = new ProvisionedThroughput(10, 5),
            Projection = new Projection
            {
                ProjectionType = ProjectionType.ALL
            },
            KeySchema = [
                new KeySchemaElement("sk", "HASH"),
                new KeySchemaElement("id", "RANGE"),
            ]
        }
    ],
});
```

### DeleteTable(DeleteTableRequest)
```csharp
migrationBuilder.DeleteTable(new DeleteTableRequest
{
    TableName = "MyTable"
});
```

### BatchWriteItem(BatchWriteItemRequest)
```csharp
migrationBuilder.BatchWriteItem(new BatchWriteItemRequest
{
    ReturnConsumedCapacity = "TOTAL",
    RequestItems = new Dictionary<string, List<WriteRequest>>
    {
        {
            "MyTable",
            [
                new WriteRequest(new PutRequest
                {
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { "id", new AttributeValue { N = "1" } },
                        { "sk", new AttributeValue { S = "Item" } },
                    }
                }),
                new WriteRequest(new PutRequest
                {
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { "id", new AttributeValue { N = "2" } },
                        { "sk", new AttributeValue { S = "Item" } },
                    }
                }),
                new WriteRequest(new PutRequest
                {
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { "id", new AttributeValue { N = "3" } },
                        { "sk", new AttributeValue { S = "Item" } },
                    }
                }),
            ]
        }
    }
});
```

### PutItems(IEnumerable\<T>)
```csharp
migrationBuilder.PutItems(new List<Item>
{
    new Item
    {
        Id = Guid.NewGuid().ToString(),
        Sk = "item",
        Name = "Item 1"
    },
    new Item
    {
        Id = Guid.NewGuid().ToString(),
        Sk = "item",
        Name = "Item 2"
    },
    new Item
    {
        Id = Guid.NewGuid().ToString(),
        Sk = "item",
        Name = "Item 3"
    }
});
```

### DeleteItems(IEnumerable\<T>)
```csharp
migrationBuilder.DeleteItems(new List<Item>
{
    new Item
    {
        Id = "E55052C6-17C5-4753-9C08-65D822D93A00",
        Sk = $"item",
        Name = $"Item 1"
    },
    new Item
    {
        Id = "7252540A-6499-4913-ADDC-34C49AF6346B",
        Sk = $"item",
        Name = $"Item 2"
    },
});
```

### Query\<T>(Func\<T, CancellationToken, Task>)
The `Query` function allows you to write a sequence of operations in order to make more complex changes.\
`T` accept DynamoDB client interface `IAmazonDynamoDB` in order to execute low level operations.\
`T` accept DynamoDB context interface `IDynamoDBContext` in order to execute high level operations.

#### Using `IAmazonDynamoDB`
```csharp
migrationBuilder.Query<IAmazonDynamoDB>(async (client, cancellationToken) =>
{
    // INSERT
    var request = new BatchWriteItemRequest
    {
        ReturnConsumedCapacity = "TOTAL",
        RequestItems = new Dictionary<string, List<WriteRequest>>
        {
            {
                "MyTable",
                [
                    new(new PutRequest
                    {
                        Item = new Dictionary<string, AttributeValue>
                        {
                            { "id", new AttributeValue { N = "1" } },
                            { "sk", new AttributeValue { S = "item" } },
                            { "name", new AttributeValue { S = "item 1" } },
                        }
                    }),
                    new(new PutRequest
                    {
                        Item = new Dictionary<string, AttributeValue>
                        {
                            { "id", new AttributeValue { N = "2" } },
                            { "sk", new AttributeValue { S = "item" } },
                            { "name", new AttributeValue { S = "item 2" } },
                        }
                    }),
                    new(new PutRequest
                    {
                        Item = new Dictionary<string, AttributeValue>
                        {
                            { "id", new AttributeValue { N = "3" } },
                            { "sk", new AttributeValue { S = "item" } },
                            { "name", new AttributeValue { S = "item 3" } },
                        }
                    }),
                ]
            }
        }
    };

    BatchWriteItemResponse response;
    do
    {
        response = await client.BatchWriteItemAsync(request, cancellationToken);
        var unprocessed = response.UnprocessedItems;
        request.RequestItems = unprocessed;
    }
    while (response.UnprocessedItems.Count > 0);
});
```

#### Using `IDynamoDBContext`
```csharp
migrationBuilder.Query<IDynamoDBContext>(async (context, cancellationToken) =>
{
    // INSERT
    var batchWrite = context.CreateBatchWrite<Item>();
    batchWrite.AddPutItems(
    [
        new Item
        {
            Id = Guid.NewGuid().ToString(),
            Sk = $"item",
            Name = $"Item 1 from context query"
        },
        new Item
        {
            Id = Guid.NewGuid().ToString(),
            Sk = $"item",
            Name = $"Item 2 from context query"
        },
        new Item
        {
            Id = "4E99E1E3-664B-4FEE-B9D5-21FC4AF2E467",
            Sk = $"item",
            Name = $"Will be deleted from context query"
        },
        new Item
        {
            Id = "F1AB5F63-9190-4177-A80B-E45277DD3646",
            Sk = $"item",
            Name = $"Will be deleted from context query"
        },
    ]);
    await batchWrite.ExecuteAsync(cancellationToken);

    // SELECT
    var itemToDelete = new List<Item>
    {
        await context.LoadAsync<Item>("4E99E1E3-664B-4FEE-B9D5-21FC4AF2E467", "item", cancellationToken),
        await context.LoadAsync<Item>("F1AB5F63-9190-4177-A80B-E45277DD3646", "item", cancellationToken),
    };

    // DELETE
    batchWrite = context.CreateBatchWrite<Item>();
    batchWrite.AddDeleteItems(itemToDelete);
    await batchWrite.ExecuteAsync(cancellationToken);
});
```