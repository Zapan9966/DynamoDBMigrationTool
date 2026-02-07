using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DynamoDBMigrationLib.Abstraction;

public abstract class DynamoDBMigrationBootstrap(IConfiguration configuration)
{
    public IConfiguration Configuration => configuration;

    public abstract void ConfigureServices(
        IServiceCollection services
    );
}