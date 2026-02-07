using McMaster.Extensions.CommandLineUtils;

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
}
