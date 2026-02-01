using Amazon.DynamoDBv2;
using Amazon.Runtime;

namespace DynamoDBMigrationLib;

public sealed class DynamoDBConfiguration
{
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? Endpoint { get; set; }
    public string? Region { get; set; }
    public bool? UseHttp { get; set; }

    public void Merge(DynamoDBConfiguration? other)
    {
        AccessKey = other?.AccessKey ?? AccessKey;
        SecretKey = other?.SecretKey ?? SecretKey;
        Endpoint = other?.Endpoint ?? Endpoint;
        Region = other?.Region ?? Region;
        UseHttp = other?.UseHttp ?? UseHttp;
    }

    public AmazonDynamoDBClient BuildClient()
    {
        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = Endpoint,
            UseHttp = UseHttp ?? false,
        };

        return !string.IsNullOrEmpty(AccessKey) && !string.IsNullOrEmpty(SecretKey)
            ? new AmazonDynamoDBClient(
                new BasicAWSCredentials(AccessKey, SecretKey),
                config
            )
            : new AmazonDynamoDBClient(config);
    }
}
