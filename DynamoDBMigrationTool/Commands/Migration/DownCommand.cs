using DynamoDBMigrationLib;
using DynamoDBMigrationTool.Extensions;
using DynamoDBMigrationTool.Helpers;
using DynamoDBMigrationTool.Services.Interface;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace DynamoDBMigrationTool.Commands.Migration;

internal class DownCommand(
    IAssemblyService assemblyService,
    IConfigurationHelperWrapper configurationHelperWrapper,
    IConsole console
) : BaseCommand
{
    private readonly IAssemblyService _assemblyService = assemblyService;
    private readonly IConfigurationHelperWrapper _configurationHelperWrapper = configurationHelperWrapper;
    private readonly IConsole _console = console;

    [Argument(0, "Migration name", Description = "Name of the migration to revert, if it's not the last migration, every migrations created after this one will be reverted.")]
    public string? Name { get; set; }

    public override async Task<int> OnExecute()
    {
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            var assemblyPath = _assemblyService.AssemblyPath(GetProjectFile());

            var configuration = _configurationHelperWrapper.LoadConfiguration(assemblyPath);
            var options = configuration.Get<MigrationToolOptions>();

            var assembly = _assemblyService.LoadAssembly(assemblyPath);
            var runner = _assemblyService.CreateRunner(assembly, configuration, assemblyPath);

            await runner.MigrateDownAsync(Name, assembly, options);
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                _console.WriteLine(ex.StackTrace);
            }
            _console.WriteLineError($"\u274C {ex.Message}");
            return 1;
        }

        return 0;
    }
}
