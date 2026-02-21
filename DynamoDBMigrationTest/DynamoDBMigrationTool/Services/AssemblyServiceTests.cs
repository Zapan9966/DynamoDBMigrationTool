using DynamoDBMigrationLib.Migrations.Interfaces;
using DynamoDBMigrationLib.Services;
using DynamoDBMigrationTest.Helpers;
using DynamoDBMigrationTool.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DynamoDBMigrationTest.DynamoDBMigrationTool.Services;

public class AssemblyServiceTests
{
    private readonly Mock<IMigrationToolConfigService> _mockConfigService;
    private readonly AssemblyService _service;

    public AssemblyServiceTests()
    {
        _mockConfigService = new Mock<IMigrationToolConfigService>();
        _service = new AssemblyService(_mockConfigService.Object);
    }

    [Fact]
    public void AssemblyPath_Should_Throw_When_Csproj_Not_Found()
    {
        // Act
        Action act = () => _service.AssemblyPath("missing.csproj");

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*csproj*");
    }

    [Fact]
    public void CreateRunner_Should_Throw_When_Assembly_Is_Null()
    {
        // Arrange
        _mockConfigService
            .Setup(c => c.LoadConfiguration(typeof(TestMigration).Assembly.Location))
            .Returns(new ConfigurationBuilder().Build());

        // Act
        Action act = () => _service.CreateRunner(null, "path.dll");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateRunner_Should_Return_MigrationRunner_When_Dependencies_Are_Resolvable()
    {
        // Arrange
        var service = new AssemblyService(_mockConfigService.Object);
        var assembly = typeof(TestBootstrap).Assembly;
        var dir = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        File.WriteAllText(
            Path.Combine(dir.FullName, "appsettings.json"),
            "{}");

        var assemblyPath = Path.Combine(dir.FullName, "dummy.dll");
        File.WriteAllBytes(assemblyPath, [0]); // fichier existant

        // Arrange
        _mockConfigService
            .Setup(c => c.LoadConfiguration(typeof(TestMigration).Assembly.Location))
            .Returns(new ConfigurationBuilder().Build());

        // Act
        var runner = service.CreateRunner(assembly, assemblyPath);

        // Assert
        runner.Should().NotBeNull();
        runner.Should().BeAssignableTo<IMigrationRunner>();
    }

    [Fact]
    public void LoadAssembly_Should_Throw_When_Path_Invalid()
    {
        // Act
        Action act = () => _service.LoadAssembly("missing.dll");

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }
}
