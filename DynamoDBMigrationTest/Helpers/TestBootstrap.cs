using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using DynamoDBMigrationLib.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DynamoDBMigrationTest.Helpers;

internal sealed class TestBootstrap(IConfiguration configuration)
    : DynamoDBMigrationBootstrap(configuration)
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAmazonDynamoDB>(_ =>
            new AmazonDynamoDBClient(
                new AmazonDynamoDBConfig
                {
                    ServiceURL = "http://localhost:8000"
                }));

        services.AddSingleton<IDynamoDBContext, DynamoDBContext>();
    }
}
