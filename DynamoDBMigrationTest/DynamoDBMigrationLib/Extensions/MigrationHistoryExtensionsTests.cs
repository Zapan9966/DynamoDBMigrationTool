using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DynamoDBMigrationLib;
using DynamoDBMigrationLib.Constants;
using DynamoDBMigrationLib.Extensions.AmazonDynamoDB;
using FluentAssertions;
using Moq;

namespace DynamoDBMigrationTest.DynamoDBMigrationLib.Extensions;

public class MigrationHistoryExtensionsTests
{
    private readonly Mock<IAmazonDynamoDB> _mockClient;

    public MigrationHistoryExtensionsTests()
    {
        _mockClient = new Mock<IAmazonDynamoDB>();
    }

    #region CreateMigrationHistoryAsync

    [Fact]
    public async Task CreateMigrationHistoryAsync_Should_Create_Table_And_Wait_Until_Active()
    {
        // Arrange
        var tableName = Constants.MIGRATION_HISTORY_TABLE;

        var createResponse = new CreateTableResponse
        {
            TableDescription = new TableDescription
            {
                TableName = tableName,
                TableStatus = TableStatus.CREATING
            }
        };

        _mockClient
            .Setup(c => c.CreateTableAsync(
                It.IsAny<CreateTableRequest>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(createResponse);

        _mockClient
            .Setup(c => c.DescribeTableAsync(
                It.Is<DescribeTableRequest>(r => r.TableName == tableName),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new DescribeTableResponse
            {
                Table = new TableDescription
                {
                    TableName = tableName,
                    TableStatus = TableStatus.ACTIVE
                }
            });

        var options = new MigrationToolOptions();

        // Act
        var act = () => _mockClient.Object.CreateMigrationHistoryAsync(options);

        // Assert
        await act.Should().NotThrowAsync();

        _mockClient.Verify(c =>
            c.CreateTableAsync(It.IsAny<CreateTableRequest>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

        _mockClient.Verify(c =>
            c.DescribeTableAsync(
                It.IsAny<DescribeTableRequest>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task CreateMigrationHistoryAsync_Should_Ignore_ResourceInUseException()
    {
        // Arrange
        _mockClient
            .Setup(c => c.CreateTableAsync(
                It.IsAny<CreateTableRequest>(),
                It.IsAny<CancellationToken>()
            ))
            .ThrowsAsync(new ResourceInUseException("Table already exists"));

        var options = new MigrationToolOptions();

        // Act
        var act = () => _mockClient.Object.CreateMigrationHistoryAsync(options);

        // Assert
        await act.Should().NotThrowAsync();

        _mockClient.Verify(c =>
            c.CreateTableAsync(It.IsAny<CreateTableRequest>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateMigrationHistoryAsync_Should_Not_Swallow_Unexpected_Exceptions()
    {
        // Arrange
        _mockClient
            .Setup(c => c.CreateTableAsync(
                It.IsAny<CreateTableRequest>(),
                It.IsAny<CancellationToken>()
            ))
            .ThrowsAsync(new InternalServerErrorException("Boom"));

        var options = new MigrationToolOptions();

        // Act
        var act = () => _mockClient.Object.CreateMigrationHistoryAsync(options);

        // Assert
        await act.Should().ThrowAsync<InternalServerErrorException>();
    }

    [Fact]
    public async Task CreateMigrationHistoryAsync_Should_Propagate_CancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _mockClient
            .Setup(c => c.CreateTableAsync(
                It.IsAny<CreateTableRequest>(),
                cts.Token
            ))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var options = new MigrationToolOptions();

        // Act
        var act = () => _mockClient.Object.CreateMigrationHistoryAsync(options, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetAppliedMigrationAsync

    [Fact]
    public async Task GetAppliedMigrationAsync_Should_Return_Migration_Names()
    {
        // Arrange
        _mockClient
            .Setup(c => c.QueryAsync(
                It.Is<QueryRequest>(r =>
                    r.TableName == Constants.MIGRATION_HISTORY_TABLE &&
                    r.KeyConditions.ContainsKey("type") &&
                    r.KeyConditions["type"].ComparisonOperator == ComparisonOperator.EQ
                ),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new QueryResponse
            {
                Items =
                [
                    new Dictionary<string, AttributeValue>
                    {
                        ["name"] = new AttributeValue { S = "001_Init" }
                    },
                    new Dictionary<string, AttributeValue>
                    {
                        ["name"] = new AttributeValue { S = "002_AddIndex" }
                    }
                ]
            });

        var options = new MigrationToolOptions();

        // Act
        var result = await _mockClient.Object.GetAppliedMigrationAsync(options);

        // Assert
        result.Should().BeEquivalentTo(
            ["001_Init", "002_AddIndex"],
            options => options.WithStrictOrdering()
        );
    }

    [Fact]
    public async Task GetAppliedMigrationAsync_Should_Ignore_Items_Without_Name()
    {
        // Arrange
        _mockClient
            .Setup(c => c.QueryAsync(
                It.IsAny<QueryRequest>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new QueryResponse
            {
                Items =
                [
                    new Dictionary<string, AttributeValue>
                    {
                        ["name"] = new AttributeValue { S = "001_Init" }
                    },
                    [],
                    new Dictionary<string, AttributeValue>
                    {
                        ["name"] = new AttributeValue { S = null }
                    }
                ]
            });

        var options = new MigrationToolOptions();

        // Act
        var result = await _mockClient.Object.GetAppliedMigrationAsync(options);

        // Assert
        result.Should().ContainSingle()
              .Which.Should().Be("001_Init");
    }

    [Fact]
    public async Task GetAppliedMigrationAsync_Should_Return_Empty_List_When_No_Items()
    {
        // Arrange
        _mockClient
            .Setup(c => c.QueryAsync(
                It.IsAny<QueryRequest>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new QueryResponse
            {
                Items = []
            });

        var options = new MigrationToolOptions();

        // Act
        var result = await _mockClient.Object.GetAppliedMigrationAsync(options);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAppliedMigrationAsync_Should_Propagate_Exceptions()
    {
        // Arrange
        _mockClient
            .Setup(c => c.QueryAsync(
                It.IsAny<QueryRequest>(),
                It.IsAny<CancellationToken>()
            ))
            .ThrowsAsync(new InternalServerErrorException("Boom"));

        var options = new MigrationToolOptions();

        // Act
        var act = () => _mockClient.Object.GetAppliedMigrationAsync(options);

        // Assert
        await act.Should().ThrowAsync<InternalServerErrorException>();
    }

    #endregion

    #region AddMigrationHistory

    [Fact]
    public async Task AddMigrationHistory_Should_Put_Migration_Item()
    {
        // Arrange
        var migrationId = "001_Init";

        _mockClient
            .Setup(c => c.PutItemAsync(
                It.Is<PutItemRequest>(r =>
                    r.TableName == Constants.MIGRATION_HISTORY_TABLE &&
                    r.Item["type"].S == "migration" &&
                    r.Item["name"].S == migrationId
                ),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new PutItemResponse());

        var options = new MigrationToolOptions();

        // Act
        await _mockClient.Object.AddMigrationHistory(migrationId, options);

        // Assert
        _mockClient.Verify(c =>
            c.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AddMigrationHistory_Should_Propagate_Exceptions()
    {
        // Arrange
        var migrationId = "001_Init";

        _mockClient
            .Setup(c => c.PutItemAsync(
                It.IsAny<PutItemRequest>(),
                It.IsAny<CancellationToken>()
            ))
            .ThrowsAsync(new ConditionalCheckFailedException("Boom"));

        var options = new MigrationToolOptions();

        // Act
        var act = () => _mockClient.Object.AddMigrationHistory(migrationId, options);

        // Assert
        await act.Should().ThrowAsync<ConditionalCheckFailedException>();
    }

    [Fact]
    public async Task AddMigrationHistory_Should_Forward_CancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _mockClient
            .Setup(c => c.PutItemAsync(
                It.IsAny<PutItemRequest>(),
                cts.Token
            ))
            .ReturnsAsync(new PutItemResponse());

        var options = new MigrationToolOptions();

        // Act
        await _mockClient.Object.AddMigrationHistory("001_Init", options, cts.Token);

        // Assert
        _mockClient.Verify(c =>
            c.PutItemAsync(It.IsAny<PutItemRequest>(), cts.Token),
            Times.Once
        );
    }

    #endregion

    #region DeleteMigrationHistory

    [Fact]
    public async Task DeleteMigrationHistory_Should_Delete_Migration_Item()
    {
        // Arrange
        var migrationId = "001_Init";

        _mockClient
            .Setup(c => c.DeleteItemAsync(
                It.Is<DeleteItemRequest>(r =>
                    r.TableName == Constants.MIGRATION_HISTORY_TABLE &&
                    r.Key["type"].S == "migration" &&
                    r.Key["name"].S == migrationId
                ),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new DeleteItemResponse());

        var options = new MigrationToolOptions();

        // Act
        await _mockClient.Object.DeleteMigrationHistory(migrationId, options);

        // Assert
        _mockClient.Verify(c =>
            c.DeleteItemAsync(It.IsAny<DeleteItemRequest>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteMigrationHistory_Should_Propagate_Exceptions()
    {
        // Arrange
        var migrationId = "001_Init";

        _mockClient
            .Setup(c => c.DeleteItemAsync(
                It.IsAny<DeleteItemRequest>(),
                It.IsAny<CancellationToken>()
            ))
            .ThrowsAsync(new ConditionalCheckFailedException("Boom"));

        var options = new MigrationToolOptions();

        // Act
        var act = () => _mockClient.Object.DeleteMigrationHistory(migrationId, options);

        // Assert
        await act.Should().ThrowAsync<ConditionalCheckFailedException>();
    }

    [Fact]
    public async Task DeleteMigrationHistory_Should_Forward_CancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _mockClient
            .Setup(c => c.DeleteItemAsync(
                It.IsAny<DeleteItemRequest>(),
                cts.Token
            ))
            .ReturnsAsync(new DeleteItemResponse());

        var options = new MigrationToolOptions();

        // Act
        await _mockClient.Object.DeleteMigrationHistory("001_Init", options, cts.Token);

        // Assert
        _mockClient.Verify(c =>
            c.DeleteItemAsync(It.IsAny<DeleteItemRequest>(), cts.Token),
            Times.Once
        );
    }

    #endregion
}
