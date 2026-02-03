using DynamoDBMigrationTool.Extensions;
using DynamoDBMigrationLib.Extensions.AmazonDynamoDB;
using DynamoDBMigrationLib.Helpers;
using McMaster.Extensions.CommandLineUtils;
using System.Text;
using System.Text.RegularExpressions;

namespace DynamoDBMigrationTool.Commands.Migration;

internal class DownCommand(IConsole console) : BaseCommand
{ 
    private readonly IConsole _console = console;

    [Argument(0, "Migration name", Description = "Name of the migration to revert, if it's not the last migration, every migrations created after this one will be reverted.")]
    public string? Name { get; set; }

    public override async Task<int> OnExecute()
    {
        Console.OutputEncoding = Encoding.UTF8;
        ConsoleHelper.WriteTitle();

        #region Search csproj in application directory

        var csprojPath = GetProjectFile();

        if (string.IsNullOrEmpty(csprojPath))
        {
            _console.WriteLineError($"\u274C Project file (csproj) not found in {ApplicationDirectory}");
            return 1;
        }

        #endregion

        #region Build application

        var (exitCode, output, error) = await BuildApplication(csprojPath);

        if (exitCode != 0)
        {
            _console.WriteLine(error);
            _console.WriteLineError("\u274C Application build failed.");
            return 1;
        }

        #endregion

        #region Load application assembly

        var assemblyMatch = Regex.Match(output, $"{GetApplicationName()} -> (.*.dll)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var assemblyPath = assemblyMatch.Groups.Values
            .FirstOrDefault(v => File.Exists(v.Value))
            ?.Value;

        if (string.IsNullOrEmpty(assemblyPath))
        {
            _console.WriteLineError("\u274C Unable de find builded assembly path.");
            return 1;
        }

        var assembly = LoadAssembly(assemblyPath);

        #endregion

        #region Create DynamoDB client     

        try
        {
            var dynamoDbClient = CreateDynamoDBClient(assemblyPath);
            await dynamoDbClient.MigrateDown(Name, assembly);
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

        #endregion

        return 0;
    }
}
