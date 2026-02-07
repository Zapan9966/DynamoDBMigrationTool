using DynamoDBMigrationLib.Abstraction;
using DynamoDBMigrationLib.Extensions;
using DynamoDBMigrationLib.Migrations.Interfaces;
using DynamoDBMigrationTool.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace DynamoDBMigrationTool.Services;

internal class AssemblyService : IAssemblyService
{
    public string AssemblyPath(string? csprojPath)
    {
        if (string.IsNullOrEmpty(csprojPath) || !File.Exists(csprojPath))
            throw new FileNotFoundException($"Project file (csproj) not found.");

        var buildOutput = BuildAssembly(csprojPath);

        var applicationName = Path.GetFileNameWithoutExtension(csprojPath);

        var assemblyMatch = Regex.Match(buildOutput, $"{applicationName} -> (.*.dll)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var assemblyPath = assemblyMatch.Groups.Values
            .FirstOrDefault(v => File.Exists(v.Value))
            ?.Value;

        return assemblyPath ?? string.Empty;
    }

    public IMigrationRunner CreateRunner(Assembly? assembly, string? assemblyPath)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrEmpty(assemblyPath);

        var configuration = LoadConfiguration(assemblyPath);

        var bootstrapType = assembly
            .GetTypes()
            .FirstOrDefault(t =>
                t.IsClass
                && !t.IsAbstract
                && t.IsSubclassOf(typeof(DynamoDBMigrationBootstrap))
            )
            ?? throw new FileNotFoundException("DynamoDBMigrationBootstrap not found in the target assembly.");

        var bootstrap = (DynamoDBMigrationBootstrap)Activator.CreateInstance(bootstrapType, configuration)!;

        var services = new ServiceCollection();
        services.AddDynamoDBMigrationTool(bootstrap);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMigrationRunner>();
    }

    public Assembly LoadAssembly(string? assemblyPath)
    {
        if (string.IsNullOrEmpty(assemblyPath) || !File.Exists(assemblyPath))
            throw new FileNotFoundException("Unable de find builded assembly path.");

        var assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;

        var loadContext = new AssemblyLoadContext("AppContext", isCollectible: true);
        loadContext.Resolving += (context, assemblyName) =>
        {
            try
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            }
            catch { /* Ignore exception and try to load assembly from dll file. */ }

            var depPath = Path.Combine(assemblyDirectory, $"{assemblyName.Name}.dll");
            return File.Exists(depPath)
                ? context.LoadFromAssemblyPath(depPath)
                : null;
        };

        using var fs = File.OpenRead(assemblyPath);
        return loadContext.LoadFromStream(fs);
    }

    #region Private Methods

    private static string BuildAssembly(string? csprojPath)
    {
        if (string.IsNullOrEmpty(csprojPath) || !File.Exists(csprojPath))
            throw new FileNotFoundException($"Project file (csproj) not found.");

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

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0 && string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException(
                string.IsNullOrEmpty(error)
                    ? "An unexpected error occured"
                    : error);
        }
        return output;
    }

    private static IConfiguration LoadConfiguration(string? assemblyPath)
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

    #endregion

}
