using Microsoft.Extensions.Configuration;

namespace DynamoDBMigrationLib.Services;

internal interface IMigrationToolConfigService
{
    IConfiguration LoadConfiguration(string? assemblyPath);
}

internal class MigrationToolConfigService : IMigrationToolConfigService
{
    public IConfiguration LoadConfiguration(string? assemblyPath)
    {
        if (string.IsNullOrEmpty(assemblyPath) || !File.Exists(assemblyPath))
            throw new FileNotFoundException("Unable de find builded assembly path.");

        var assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;

        if (string.IsNullOrEmpty(assemblyDirectory) || !Directory.Exists(assemblyDirectory))
            throw new DirectoryNotFoundException("Assembly directory not found.");

        var appsettingsFiles = Directory.GetFiles(assemblyDirectory, "appsettings.*json")
            .Select(f => Path.GetFileName(f))
            .OrderBy(f => f);

        if (appsettingsFiles.Any())
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            return new ConfigurationBuilder()
                .SetBasePath(assemblyDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }
        throw new FileNotFoundException("Assembly appsettings.json not found.");
    }
}
