using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using DynamoDBMigrationLib.Extensions;
using DynamoDBMigrationLib.Migrations;
using DynamoDBMigrationLib.Migrations.Interfaces;
using DynamoDBMigrationTest.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DynamoDBMigrationTest.DynamoDBMigrationLib.Extensions;

public class ServiceCollectionExtensionsTests
{
    private readonly Mock<IAmazonDynamoDB> _mockClient;
    private readonly Mock<IDynamoDBContext> _mockContext;

    public ServiceCollectionExtensionsTests()
    {
        _mockClient = new Mock<IAmazonDynamoDB>();
        _mockContext = new Mock<IDynamoDBContext>();
    }

    [Fact]
    public void ServiceProvider_Should_Resolve_IMigrationRunner_With_Dependencies()
    {
        // Arrange
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder().Build();
        var bootstrap = new TestBootstrap(configuration);

        services
            .AddSingleton(_mockClient.Object)
            .AddSingleton(_mockContext.Object)
            .AddDynamoDBMigrationTool(bootstrap);

        // Act
        using var provider = services.BuildServiceProvider();
        var act = provider.GetRequiredService<IMigrationRunner>;

        // Assert
        act.Should().NotThrow();
        provider.GetRequiredService<IMigrationRunner>()
            .Should()
            .BeOfType<MigrationRunner>();
    }
}

