using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using DynamoDBMigrationLib.Constants;
using DynamoDBMigrationLib.Migrations;
using DynamoDBMigrationTest.Helpers;
using FluentAssertions;
using Moq;

namespace DynamoDBMigrationTest.DynamoDBMigrationLib.Migrations;

public class MigrationRunnerTests
{
    private readonly Mock<IAmazonDynamoDB> _mockClient;
    private readonly Mock<IDynamoDBContext> _mockContext;

    public MigrationRunnerTests()
    {
        _mockClient = new Mock<IAmazonDynamoDB>();
        _mockContext = new Mock<IDynamoDBContext>();

        _mockClient.Setup(c => c.CreateTableAsync(
                It.IsAny<CreateTableRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateTableResponse
            {
                TableDescription = new TableDescription
                {
                    TableStatus = TableStatus.ACTIVE,
                    TableName = Constants.MIGRATION_HISTORY_TABLE
                }
            });

        _mockClient.Setup(c => c.DescribeTableAsync(
                It.IsAny<DescribeTableRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DescribeTableResponse
            {
                Table = new TableDescription
                {
                    TableStatus = TableStatus.ACTIVE,
                    TableName = Constants.MIGRATION_HISTORY_TABLE
                }
            });
    }

    [Fact]
    public async Task MigrateAsync_Should_Apply_Migration_When_Not_Applied()
    {
        // Arrange
        TestMigration.ExecutionCount = 0;

        _mockClient
            .Setup(c => c.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResponse { Items = [] });

        _mockClient
            .Setup(c => c.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutItemResponse());

        var runner = new MigrationRunner(_mockClient.Object, _mockContext.Object);

        // Act
        await runner.MigrateAsync(typeof(TestMigration).Assembly);

        // Assert
        TestMigration.ExecutionCount.Should().Be(1);

        _mockClient.Verify(c =>
            c.PutItemAsync(
                It.Is<PutItemRequest>(r => r.Item["name"].S == "001"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MigrateAsync_Should_Not_Reapply_Migration()
    {
        // Arrange
        TestMigration.ExecutionCount = 0;

        _mockClient
            .Setup(c => c.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResponse
            {
                Items =
                [
                    new Dictionary<string, AttributeValue>
                    {
                        ["name"] = new AttributeValue("001")
                    },
                    new Dictionary<string, AttributeValue>
                    {
                        ["name"] = new AttributeValue("002")
                    },
                    new Dictionary<string, AttributeValue>
                    {
                        ["name"] = new AttributeValue("003")
                    },
                    new Dictionary<string, AttributeValue>
                    {
                        ["name"] = new AttributeValue("999")
                    },
                ]
            });

        var runner = new MigrationRunner(_mockClient.Object, _mockContext.Object);

        // Act
        await runner.MigrateAsync(typeof(TestMigration).Assembly);

        // Assert
        TestMigration.ExecutionCount.Should().Be(0);

        _mockClient.Verify(c => 
            c.PutItemAsync(
                It.IsAny<PutItemRequest>(), 
                It.IsAny<CancellationToken>()
            ), 
            Times.Never
        );
    }

    [Fact]
    public async Task MigrateDownAsync_Should_Revert_Last_Migration()
    {
        // Arrange
        TestMigration.ExecutionCount = 1;

        _mockClient
            .Setup(c => c.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResponse
            {
                Items =
                [
                    new Dictionary<string, AttributeValue>
                    {
                        ["name"] = new AttributeValue("001")
                    }
                ]
            });

        _mockClient
            .Setup(c => c.DeleteItemAsync(It.IsAny<DeleteItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteItemResponse());

        var runner = new MigrationRunner(_mockClient.Object, _mockContext.Object);

        // Act
        await runner.MigrateDownAsync(null, typeof(TestMigration).Assembly);

        // Assert
        TestMigration.ExecutionCount.Should().Be(0);

        _mockClient.Verify(c =>
            c.DeleteItemAsync(
                It.Is<DeleteItemRequest>(r => r.Key["name"].S == "001"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MigrateDownAsync_With_Unknown_Migration_Should_Throw()
    {
        // Arrange
        _mockClient
            .Setup(c => c.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResponse
            {
                Items =
                [
                    new Dictionary<string, AttributeValue>
                    {
                        ["name"] = new AttributeValue("001")
                    }
                ]
            });

        var runner = new MigrationRunner(_mockClient.Object, _mockContext.Object);

        // Act
        Func<Task> act = () =>
            runner.MigrateDownAsync("DoesNotExist", typeof(TestMigration).Assembly);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

}
