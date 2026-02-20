using DynamoDBMigrationLib.Migrations.Interfaces;
using DynamoDBMigrationTest.Helpers;
using DynamoDBMigrationTool.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DynamoDBMigrationTest.DynamoDBMigrationTool.Services;

public class AssemblyServiceTests
{
    private readonly AssemblyService _service;

    public AssemblyServiceTests()
    {
        _service = new AssemblyService();
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
        // Act
        Action act = () => _service.CreateRunner(null, It.IsAny<IConfiguration>(), "path.dll");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateRunner_Should_Throw_When_AssemblyPath_Is_Null()
    {
        // Act
        Action act = () => _service.CreateRunner(typeof(TestBootstrap).Assembly, It.IsAny<IConfiguration>(), null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateRunner_Should_Return_MigrationRunner_When_Dependencies_Are_Resolvable()
    {
        // Arrange
        var service = new AssemblyService();
        var assembly = typeof(TestBootstrap).Assembly;
        var dir = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        File.WriteAllText(
            Path.Combine(dir.FullName, "appsettings.json"),
            "{}");

        var assemblyPath = Path.Combine(dir.FullName, "dummy.dll");
        File.WriteAllBytes(assemblyPath, [0]); // fichier existant

        // Act
        var runner = service.CreateRunner(assembly, It.IsAny<IConfiguration>(), assemblyPath);

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
