using McMaster.Extensions.CommandLineUtils;

namespace DynamoDBMigrationTool.Commands.Migration;

[Subcommand(typeof(AddCommand))]
[Subcommand(typeof(UpCommand))]
[Subcommand(typeof(DownCommand))]
internal sealed class MigrationCommand
{
    public int OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return 0;
    }
}
