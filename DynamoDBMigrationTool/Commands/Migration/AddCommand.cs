using DynamoDBMigrationTool.Constants;
using DynamoDBMigrationTool.Extensions;
using DynamoDBMigrationLib.Helpers;
using Fluid;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DynamoDBMigrationTool.Commands.Migration;

internal sealed class AddCommand(IConsole console) : BaseCommand
{
    private readonly IConsole _console = console;

    [Required(ErrorMessage = "Name of the migration is required.")]
    [Argument(0, "Migration name")]
    public string Name { get; set; } = string.Empty;

    [Option("-o|--output", Description = "The output directory where migrations will be stored.")]
    public string OutputFolder { get; set; } = "Migrations";

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

        #region Create migration directory

        var migrationDirectory = Path.Combine(ApplicationDirectory, OutputFolder);
        if (!Directory.Exists(migrationDirectory))
        {
            try
            {
                _console.WriteLine($"Creating migration folder to {migrationDirectory}");
                Directory.CreateDirectory(migrationDirectory);

                var project = ProjectRootElement.Open(csprojPath);
                var itemGroup = project.AddItemGroup();
                itemGroup.AddItem("Folder", OutputFolder + "\\");
                project.Save(csprojPath);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    _console.WriteLine(ex.StackTrace);
                }
                _console.WriteLineError(ex.Message);
                return 1;
            }
        }
        _console.WriteLine($"Output Folder: {migrationDirectory}");

        #endregion

        #region Check if migration already exists

        var migrationName = Sanitize(Name);
        _console.WriteLine($"Migration name: {migrationName}");
        _console.WriteLine();

        if (await MigrationExists(migrationDirectory, migrationName))
        {
            _console.WriteLineError($"\u274C A migration named {migrationName} already exists.");
            return 1;
        }

        #endregion

        #region Generate migration file

        try
        {
            var migrationId = $"{DateTime.Now:yyyyMMddHHmmss}_{migrationName}";

            var fluidParser = new FluidParser();
            var template = fluidParser.Parse(Templates.MIGRATION);

            var context = new TemplateContext(new
            {
                Namespace = Sanitize($"{GetApplicationName()}.{OutputFolder}", "."),
                MigrationId = migrationId,
                MigrationName = migrationName
            });
            var migrationClass = await template.RenderAsync(context);

            var filePath = Path.Combine(migrationDirectory, $"{migrationId}.cs");
            using var sw = File.CreateText(filePath);
            await sw.WriteAsync(migrationClass);

            _console.WriteSuccess("\u2705 ");
            _console.WriteLine($"Migration file created: {migrationId}.cs");
            _console.WriteLine();
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

    private static async Task<bool> MigrationExists(string migrationDirectory, string migrationName)
    {
        var result = false;
        var migrationFiles = Directory.GetFiles(migrationDirectory, "*.cs");
        foreach (var migrationFile in migrationFiles)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(migrationFile));
            if (await tree.GetRootAsync() is CompilationUnitSyntax root)
            {
                ClassDeclarationSyntax? @class = null;
                if (root.Members[0] is FileScopedNamespaceDeclarationSyntax @namespace)
                {
                    if (@namespace.Members[0] is ClassDeclarationSyntax classSyntax)
                    {
                        @class = classSyntax;
                    }
                }
                else if (root.Members[0] is ClassDeclarationSyntax classSyntax)
                {
                    @class = classSyntax;
                }

                if (@class != null && @class.Identifier.ValueText == migrationName)
                {
                    result = true;
                    break;
                }
            }
        }
        return result;
    }
}
