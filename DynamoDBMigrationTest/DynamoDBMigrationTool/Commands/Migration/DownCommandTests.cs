using DynamoDBMigrationLib.Migrations.Interfaces;
using DynamoDBMigrationTest.Helpers;
using DynamoDBMigrationTool.Commands.Migration;
using DynamoDBMigrationTool.Services.Interface;
using FluentAssertions;
using McMaster.Extensions.CommandLineUtils;
using Moq;

namespace DynamoDBMigrationTest.DynamoDBMigrationTool.Commands.Migration;

public class DownCommandTests
{
    private readonly Mock<IAssemblyService> _mockAssemblyService;
    private readonly Mock<IMigrationRunner> _mockRunner;

    public DownCommandTests()
    {
        _mockAssemblyService = new Mock<IAssemblyService>(MockBehavior.Strict);
        _mockRunner = new Mock<IMigrationRunner>();
    }

    private static (Mock<IConsole> Console, StringWriter Out, StringWriter Error) CreateConsole()
    {
        var outWriter = new StringWriter();
        var errorWriter = new StringWriter();

        var console = new Mock<IConsole>(MockBehavior.Loose);
        console.SetupGet(c => c.Out).Returns(outWriter);
        console.SetupGet(c => c.Error).Returns(errorWriter);

        return (console, outWriter, errorWriter);
    }

    [Fact]
    public async Task OnExecute_Should_Return_1_When_Csproj_Not_Found()
    {
        // Arrange
        var dir = FilesyStemHelper.CreateTempDir();
        var (console, _, _) = CreateConsole();

        var assemblyService = new Mock<IAssemblyService>(MockBehavior.Strict);

        var command = new DownCommand(assemblyService.Object, console.Object)
        {
            ApplicationDirectory = dir
        };

        // Act
        var result = await command.OnExecute();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task OnExecute_Should_Return_1_When_AssemblyService_Throws()
    {
        // Arrange
        var dir = FilesyStemHelper.CreateTempDir();
        FilesyStemHelper.CreateCsproj(dir);

        var (console, _, _) = CreateConsole();

        _mockAssemblyService
            .Setup(s => s.AssemblyPath(It.IsAny<string?>()))
            .Throws(new InvalidOperationException("Build failed"));

        var command = new DownCommand(_mockAssemblyService.Object, console.Object)
        {
            ApplicationDirectory = dir
        };

        // Act
        var result = await command.OnExecute();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task OnExecute_Should_Run_MigrateDown_Without_Name()
    {
        // Arrange
        var dir = FilesyStemHelper.CreateTempDir();
        FilesyStemHelper.CreateCsproj(dir);

        var (console, _, _) = CreateConsole();

        var assembly = typeof(DownCommandTests).Assembly;
        var assemblyPath = Path.Combine(dir, "TestApp.dll");

        _mockRunner
            .Setup(r => r.MigrateDownAsync(null, assembly, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAssemblyService
            .Setup(s => s.AssemblyPath(It.IsAny<string?>()))
            .Returns(assemblyPath);

        _mockAssemblyService
            .Setup(s => s.LoadAssembly(assemblyPath))
            .Returns(assembly);

        _mockAssemblyService
            .Setup(s => s.CreateRunner(assembly, assemblyPath))
            .Returns(_mockRunner.Object);

        var command = new DownCommand(_mockAssemblyService.Object, console.Object)
        {
            ApplicationDirectory = dir,
            Name = null
        };

        // Act
        var result = await command.OnExecute();

        // Assert
        result.Should().Be(0);
        _mockRunner.Verify(r =>
            r.MigrateDownAsync(null, assembly, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnExecute_Should_Run_MigrateDown_With_Name()
    {
        // Arrange
        var dir = FilesyStemHelper.CreateTempDir();
        FilesyStemHelper.CreateCsproj(dir);

        var (console, _, _) = CreateConsole();

        var assembly = typeof(DownCommandTests).Assembly;
        var assemblyPath = Path.Combine(dir, "TestApp.dll");

        _mockRunner
            .Setup(r => r.MigrateDownAsync("InitialMigration", assembly, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAssemblyService
            .Setup(s => s.AssemblyPath(It.IsAny<string?>()))
            .Returns(assemblyPath);

        _mockAssemblyService
            .Setup(s => s.LoadAssembly(assemblyPath))
            .Returns(assembly);

        _mockAssemblyService
            .Setup(s => s.CreateRunner(assembly, assemblyPath))
            .Returns(_mockRunner.Object);

        var command = new DownCommand(_mockAssemblyService.Object, console.Object)
        {
            ApplicationDirectory = dir,
            Name = "InitialMigration"
        };

        // Act
        var result = await command.OnExecute();

        // Assert
        result.Should().Be(0);
        _mockRunner.Verify(r =>
            r.MigrateDownAsync("InitialMigration", assembly, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnExecute_Should_Return_1_When_MigrateDown_Fails()
    {
        // Arrange
        var dir = FilesyStemHelper.CreateTempDir();
        FilesyStemHelper.CreateCsproj(dir);

        var (console, _, _) = CreateConsole();

        var assembly = typeof(DownCommandTests).Assembly;
        var assemblyPath = Path.Combine(dir, "TestApp.dll");

        _mockRunner
            .Setup(r => r.MigrateDownAsync(It.IsAny<string?>(), assembly, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Revert failed"));

        _mockAssemblyService
            .Setup(s => s.AssemblyPath(It.IsAny<string?>()))
            .Returns(assemblyPath);

        _mockAssemblyService
            .Setup(s => s.LoadAssembly(assemblyPath))
            .Returns(assembly);

        _mockAssemblyService
            .Setup(s => s.CreateRunner(assembly, assemblyPath))
            .Returns(_mockRunner.Object);

        var command = new DownCommand(_mockAssemblyService.Object, console.Object)
        {
            ApplicationDirectory = dir
        };

        // Act
        var result = await command.OnExecute();

        // Assert
        result.Should().Be(1);
    }

}
