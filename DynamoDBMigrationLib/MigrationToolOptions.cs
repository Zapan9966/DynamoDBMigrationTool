namespace DynamoDBMigrationLib;

internal class MigrationToolOptions
{
    public bool CreateHistoryTable { get; set; } = true;
    public string HistoryTable { get; set; } = Constants.Constants.MIGRATION_HISTORY_TABLE;
}
