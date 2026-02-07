using DynamoDBMigrationLib.Helpers;
using DynamoDBMigrationTool.Commands.Migration;
using DynamoDBMigrationTool.Services;
using DynamoDBMigrationTool.Services.Interface;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DynamoDBMigrationTool;

[Command("dynamodb")]
[VersionOptionFromMember("--version", MemberName = nameof(Version))]
[Subcommand(typeof(MigrationCommand))]
internal class Program
{
    public static Assembly? Assembly { get; set; } = null;
    public static string Version => $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0"}";

    public static int Main(string[] args)
    {
        ConsoleHelper.WriteTitle();

        var services = new ServiceCollection();

        services
            .AddSingleton<IAssemblyService, AssemblyService>()
            .AddSingleton(PhysicalConsole.Singleton);

        var serviceProvier = services.BuildServiceProvider();

        var app = new CommandLineApplication<Program>();
        app.Conventions
            .UseDefaultConventions()
            .UseConstructorInjection(serviceProvier);

        return app.Execute(args);
    }

    public int OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return 0;
    }
}
