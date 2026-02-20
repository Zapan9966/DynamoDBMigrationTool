using DynamoDBMigrationLib;
using DynamoDBMigrationLib.Migrations.Interfaces;
using DynamoDBMigrationTest.Helpers;
using DynamoDBMigrationTool.Commands.Migration;
using DynamoDBMigrationTool.Helpers;
using DynamoDBMigrationTool.Services.Interface;
using FluentAssertions;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DynamoDBMigrationTest.DynamoDBMigrationTool.Commands.Migration;

public class UpCommandTests
{
    private readonly Mock<IAssemblyService> _mockAssemblyService;
    private readonly Mock<IInternalMigrationRunner> _mockRunner;
    private readonly Mock<IConfigurationHelperWrapper> _mockIConfigurationHelperWrapper;

    public UpCommandTests()
    {
        _mockAssemblyService = new Mock<IAssemblyService>(MockBehavior.Strict);
        _mockRunner = new Mock<IInternalMigrationRunner>();
        _mockIConfigurationHelperWrapper = new Mock<IConfigurationHelperWrapper>();
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

        var command = new UpCommand(
            _mockAssemblyService.Object, 
            _mockIConfigurationHelperWrapper.Object, 
            console.Object
        )
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

        var command = new UpCommand(
            _mockAssemblyService.Object, 
            _mockIConfigurationHelperWrapper.Object, 
            console.Object
        )
        {
            ApplicationDirectory = dir
        };

        // Act
        var result = await command.OnExecute();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task OnExecute_Should_Run_Migrations_When_Valid()
    {
        // Arrange
        var dir = FilesyStemHelper.CreateTempDir();
        FilesyStemHelper.CreateCsproj(dir);

        var (console, _, _) = CreateConsole();

        var assembly = typeof(UpCommandTests).Assembly;
        var assemblyPath = Path.Combine(dir, "TestApp.dll");

        _mockAssemblyService
            .Setup(s => s.AssemblyPath(It.IsAny<string?>()))
            .Returns(assemblyPath);

        _mockAssemblyService
            .Setup(s => s.LoadAssembly(assemblyPath))
            .Returns(assembly);

        _mockAssemblyService.
            Setup(s => s.CreateRunner(assembly, It.IsAny<IConfiguration>(), assemblyPath))
            .Returns(_mockRunner.Object);

        _mockIConfigurationHelperWrapper
               .Setup(s => s.LoadConfiguration(assemblyPath))
               .Returns(new ConfigurationBuilder().Build());

        _mockRunner
            .Setup(r => r.MigrateAsync(assembly, It.IsAny<MigrationToolOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new UpCommand(
            _mockAssemblyService.Object, 
            _mockIConfigurationHelperWrapper.Object, 
            console.Object
        )
        {
            ApplicationDirectory = dir
        };

        // Act
        var result = await command.OnExecute();

        // Assert
        result.Should().Be(0);
        _mockRunner.Verify(r =>
            r.MigrateAsync(assembly, It.IsAny<MigrationToolOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnExecute_Should_Return_1_When_MigrateAsync_Fails()
    {
        // Arrange
        var dir = FilesyStemHelper.CreateTempDir();
        FilesyStemHelper.CreateCsproj(dir);

        var (console, _, _) = CreateConsole();

        var assembly = typeof(UpCommandTests).Assembly;
        var assemblyPath = Path.Combine(dir, "TestApp.dll");

        _mockRunner
            .Setup(r => r.MigrateAsync(assembly, It.IsAny<MigrationToolOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Migration failed"));

        _mockAssemblyService
            .Setup(s => s.AssemblyPath(It.IsAny<string?>()))
            .Returns(assemblyPath);

        _mockAssemblyService
            .Setup(s => s.LoadAssembly(assemblyPath))
            .Returns(assembly);

        _mockAssemblyService
            .Setup(s => s.CreateRunner(assembly, It.IsAny<IConfiguration>(), assemblyPath))
            .Returns(_mockRunner.Object);

        var command = new UpCommand(
            _mockAssemblyService.Object, 
            _mockIConfigurationHelperWrapper.Object, 
            console.Object
        )
        {
            ApplicationDirectory = dir
        };

        // Act
        var result = await command.OnExecute();

        // Assert
        result.Should().Be(1);
    }
}
