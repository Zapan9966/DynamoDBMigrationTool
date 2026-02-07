using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using DynamoDBMigrationLib.Migrations;
using FluentAssertions;
using Moq;

namespace DynamoDBMigrationTest.DynamoDBMigrationLib.Migrations;

public class MigrationBuilderTests
{
    private readonly Mock<IAmazonDynamoDB> _mockClient;
    private readonly Mock<IDynamoDBContext> _mockContext;

    public MigrationBuilderTests()
    {
        _mockClient = new Mock<IAmazonDynamoDB>();
        _mockContext = new Mock<IDynamoDBContext>();
    }

    [Fact]
    public void CreateTable_Should_Add_DynamoDb_Operation()
    {
        // Arrange
        var builder = new MigrationBuilder();
        var request = new CreateTableRequest { TableName = "TestTable" };

        // Act
        builder.CreateTable(request);

        // Assert
        builder.Should().ContainSingle();

        var operation = builder.Single();

        operation.DynamoDBType.Should().Be(typeof(IAmazonDynamoDB));
    }

    [Fact]
    public void DeleteTable_Should_Add_DynamoDb_Operation()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.DeleteTable(new DeleteTableRequest());

        // Assert
        builder.Single().DynamoDBType
            .Should().Be(typeof(IAmazonDynamoDB));
    }

    [Fact]
    public void BatchWriteItem_Should_Add_DynamoDb_Operation()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.BatchWriteItem(new BatchWriteItemRequest());

        // Assert
        builder.Single().DynamoDBType
            .Should().Be(typeof(IAmazonDynamoDB));
    }

    [Fact]
    public void PutItems_Should_Add_Context_Operation()
    {
        // Arrange
        var builder = new MigrationBuilder();
        var items = new[] { new TestEntity() };

        // Act
        builder.PutItems(items);

        // Assert
        builder.Single().DynamoDBType
            .Should().Be(typeof(IDynamoDBContext));
    }

    [Fact]
    public void DeleteItems_Should_Add_Context_Operation()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.DeleteItems(new[] { new TestEntity() });

        // Assert
        builder.Single().DynamoDBType
            .Should().Be(typeof(IDynamoDBContext));
    }

    [Fact]
    public async Task Query_With_IAmazonDynamoDB_Should_Execute()
    {
        // Arrange
        var builder = new MigrationBuilder();

        builder.Query<IAmazonDynamoDB>((_, _) => Task.CompletedTask);

        var operation = builder.Single();

        // Act
        await operation.Execute(_mockClient.Object, CancellationToken.None);

        // Assert
        operation.DynamoDBType.Should().Be(typeof(IAmazonDynamoDB));
    }

    [Fact]
    public async Task Query_With_IDynamoDBContext_Should_Execute()
    {
        // Arrange
        var builder = new MigrationBuilder();

        builder.Query<IDynamoDBContext>((_, _) => Task.CompletedTask);

        var operation = builder.Single();

        // Act
        await operation.Execute(_mockContext.Object, CancellationToken.None);

        // Assert
        operation.DynamoDBType.Should().Be(typeof(IDynamoDBContext));
    }

    [Fact]
    public async Task Query_With_Invalid_Type_Should_Throw_NotSupportedException()
    {
        // Arrange
        var builder = new MigrationBuilder();

        builder.Query<string>((_, _) => Task.CompletedTask);

        var operation = builder.Single();

        // Act
        Func<Task> act = () =>
            operation.Execute("invalid", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*not a valid type*");
    }

    private sealed class TestEntity
    {
        public string Id { get; set; } = string.Empty;
    }

}
