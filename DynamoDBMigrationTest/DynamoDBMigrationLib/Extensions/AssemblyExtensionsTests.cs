using DynamoDBMigrationLib.Extensions;
using DynamoDBMigrationTest.Helpers;
using FluentAssertions;

namespace DynamoDBMigrationTest.DynamoDBMigrationLib.Extensions;

public class AssemblyExtensionsTests
{
    [Fact]
    public void GetMigrationsDefinitions_Should_Return_Only_Concrete_Migrations_With_Attribute()
    {
        // Arrange
        var assembly = typeof(AssemblyExtensionsTests).Assembly;

        // Act
        var result = assembly.GetMigrationsDefinitions();

        // Assert
        result.Values.Should().OnlyContain(m => 
            m is TestMigration 
            || m is FirstMigration 
            || m is SecondMigration
        );
    }

    [Fact]
    public void GetMigrationsDefinitions_Should_Use_MigrationAttribute_Id_As_Key()
    {
        // Arrange
        var assembly = typeof(AssemblyExtensionsTests).Assembly;

        // Act
        var result = assembly.GetMigrationsDefinitions();

        // Assert
        result.Should().ContainKey("001");
        result.Should().ContainKey("002");
    }

    [Fact]
    public void GetMigrationsDefinitions_Should_Create_Migration_Instances()
    {
        // Arrange
        var assembly = typeof(AssemblyExtensionsTests).Assembly;

        // Act
        var result = assembly.GetMigrationsDefinitions();

        // Assert
        result["001"].Should().BeOfType<TestMigration>();
        result["002"].Should().BeOfType<FirstMigration>();
        result["003"].Should().BeOfType<SecondMigration>();
    }

    [Fact]
    public void GetMigrationsDefinitions_Should_Return_Migrations_Sorted_By_Id()
    {
        // Arrange
        var assembly = typeof(FirstMigration).Assembly;

        // Act
        var result = assembly.GetMigrationsDefinitions();

        // Assert
        result.Keys.Should().ContainInOrder("001", "002");
    }
}
