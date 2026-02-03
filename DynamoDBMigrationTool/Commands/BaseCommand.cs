using Amazon.DynamoDBv2;
using DynamoDBMigrationLib;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace DynamoDBMigrationTool.Commands;

internal abstract class BaseCommand
{
    [DirectoryExists]
    [Option("-r|--root", Description = "Root directory of the application containing migrations.")]
    internal string ApplicationDirectory { get; set; } = Directory.GetCurrentDirectory();

    protected string? GetProjectFile()
        => Directory
            .GetFiles(ApplicationDirectory, "*.csproj")
            .FirstOrDefault();

    public abstract Task<int> OnExecute();

    protected string? GetApplicationName()
        => Path.GetFileNameWithoutExtension(GetProjectFile());

    protected static string Sanitize(string stringToSanitize, string replace = "_")
    {
        var removeChars = new HashSet<char>(" ?&^$#@!()+-,:;<>’\\\'-_*");
        var result = new StringBuilder(stringToSanitize.Length);

        foreach (char c in stringToSanitize)
        {
            result.Append(!removeChars.Contains(c) ? c : replace);
        }
        return result.ToString();
    }

    protected static async Task<(int ExitCode, string Output, string Error)> BuildApplication(string csprojPath)
    {
        var process = new Process
        {
            StartInfo = new()
            {
                FileName = "dotnet",
                Arguments = $"build \"{csprojPath}\" -c Release",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 && string.IsNullOrEmpty(error))
        {
            error = "An unexpected error occured";
        }

        return (process.ExitCode, output, error);
    }

    protected static Assembly LoadAssembly(string assemblyPath)
    {
        var assemblyDirectory = Path.GetDirectoryName(assemblyPath);

        var loadContext = new AssemblyLoadContext("AppContext", isCollectible: true);
        loadContext.Resolving += (context, assemblyName) =>
        {
            try
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            }
            catch 
            { 
                // Ignore exception and try to load assembly from dll file.
            }

            var depPath = Path.Combine(assemblyDirectory!, $"{assemblyName.Name}.dll");
            return File.Exists(depPath)
                ? context.LoadFromAssemblyPath(depPath)
                : null;
        };

        using var fs = File.OpenRead(assemblyPath);
        return loadContext.LoadFromStream(fs);
    }

    protected static AmazonDynamoDBClient CreateDynamoDBClient(string assemblyPath)
    {
        var assemblyDirectory = Path.GetDirectoryName(assemblyPath);

        if (!string.IsNullOrEmpty(assemblyDirectory))
        {
            var appsettingsFiles = Directory.GetFiles(assemblyDirectory, "appsettings.*json")
                .Select(f => Path.GetFileName(f))
                .OrderBy(f => f);

            if (appsettingsFiles.Any())
            {
                var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(assemblyDirectory)
                    .AddJsonFile("appsettings.json", false, true)
                    .AddJsonFile($"appsettings.{environmentName}.json", true)
                    .AddEnvironmentVariables()
                    .Build();

                DynamoDBConfiguration dynamoDBConfiguration = new();
                var configKeys = configuration.AsEnumerable()
                    .Where(kp => kp.Value == null)
                    .Select(kp => kp.Key);

                foreach (var key in configKeys)
                {
                    dynamoDBConfiguration.Merge(configuration?.GetSection(key).Get<DynamoDBConfiguration>());
                }

                return dynamoDBConfiguration.BuildClient();
            }
        }

        return new AmazonDynamoDBClient();
    }
}
