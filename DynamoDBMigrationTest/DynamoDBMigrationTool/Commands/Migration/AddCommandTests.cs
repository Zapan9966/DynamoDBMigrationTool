using DynamoDBMigrationTest.Helpers;
using DynamoDBMigrationTool.Commands.Migration;
using FluentAssertions;
using McMaster.Extensions.CommandLineUtils;
using Moq;

namespace DynamoDBMigrationTest.DynamoDBMigrationTool.Commands.Migration;

public class AddCommandTests
{
    private readonly Mock<IConsole> _mockConsole;

    public AddCommandTests()
    {
        var outWriter = new StringWriter();
        var errorWriter = new StringWriter();

        _mockConsole = new Mock<IConsole>(MockBehavior.Loose);

        _mockConsole.SetupGet(c => c.Out).Returns(outWriter);
        _mockConsole.SetupGet(c => c.Error).Returns(errorWriter);
    }

    private static (AddCommand Command, StringWriter Out, StringWriter Error) CreateCommand(string directory)
    {
        var outWriter = new StringWriter();
        var errorWriter = new StringWriter();

        var console = new Mock<IConsole>(MockBehavior.Loose);
        console.SetupGet(c => c.Out).Returns(outWriter);
        console.SetupGet(c => c.Error).Returns(errorWriter);

        var command = new AddCommand(console.Object)
        {
            ApplicationDirectory = directory,
            Name = "InitialMigration"
        };

        return (command, outWriter, errorWriter);
    }

    [Fact]
    public async Task OnExecute_Should_Fail_When_Csproj_Not_Found()
    {
        // Arrange
        var dir = FilesyStemHelper.CreateTempDir();
        var (command, outWriter, error) = CreateCommand(dir);

        // Act
        var result = await command.OnExecute();
        var output = outWriter.ToString() + error.ToString();

        // Assert
        result.Should().Be(1);
        output.ToString().Should().Contain("csproj");
    }

    [Fact]
    public async Task OnExecute_Should_Create_Migration_Directory_When_Missing()
    {
        // Arrange
        var dir = FilesyStemHelper.CreateTempDir();
        FilesyStemHelper.CreateCsproj(dir);

        var output = "Migrations";

        var command = new AddCommand(_mockConsole.Object)
        {
            ApplicationDirectory = dir,
            Name = "InitialMigration",
            OutputFolder = output
        };

        // Act
        var result = await command.OnExecute();

        // Assert
        result.Should().Be(0);
        Directory.Exists(Path.Combine(dir, output)).Should().BeTrue();
    }

    [Fact]
    public async Task OnExecute_Should_Fail_When_Migration_Already_Exists()
    {
        // Arrange
        var dir = FilesyStemHelper.CreateTempDir();
        FilesyStemHelper.CreateCsproj(dir);

        var migrationsDir = Path.Combine(dir, "Migrations");
        Directory.CreateDirectory(migrationsDir);

        await File.WriteAllTextAsync(
            Path.Combine(migrationsDir, "20240101000000_TestMigration.cs"),
            """
            namespace Test.Migrations;
            public class TestMigration { }
            """
        );

        // Act
        var (command, _, _) = CreateCommand(dir);
        command.Name = "TestMigration";

        var result = await command.OnExecute();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task OnExecute_Should_Create_Migration_File_When_Valid()
    {
        // Arrange
        var dir = FilesyStemHelper.CreateTempDir();
        FilesyStemHelper.CreateCsproj(dir);

        var command = new AddCommand(_mockConsole.Object)
        {
            ApplicationDirectory = dir,
            Name = "CreateUsersTable"
        };

        // Act
        var result = await command.OnExecute();

        // Assert
        result.Should().Be(0);

        var migrationsDir = Path.Combine(dir, "Migrations");
        var files = Directory.GetFiles(migrationsDir, "*.cs");
        var fileText = await File.ReadAllTextAsync(files[0]);

        files.Should().HaveCount(1);
        fileText.Should().Contain("CreateUsersTable");
    }

    [Fact]
    public async Task OnExecute_Should_Sanitize_Migration_Name()
    {
        // Arrange
        var dir = FilesyStemHelper.CreateTempDir();
        FilesyStemHelper.CreateCsproj(dir);

        var command = new AddCommand(_mockConsole.Object)
        {
            ApplicationDirectory = dir,
            Name = "Create Users-Table!"
        };

        // Act
        await command.OnExecute();

        // Assert
        var file = Directory.GetFiles(Path.Combine(dir, "Migrations")).Single();
        file.Should().Contain("Create_Users_Table");
    }

}
