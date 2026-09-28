using Amazon.Runtime;
using Amazon.SQS;
using Testcontainers.LocalStack;

namespace Api.Project.Template.Tests.Integration.Messaging.Sqs;

/// <summary>
/// One LocalStack container shared by the SQS tests; each test uses its own uniquely named queue.
/// </summary>
public sealed class LocalStackFixture : IAsyncLifetime
{
    public const string Region = "us-east-1";
    public const string AccessKey = "test";
    public const string SecretKey = "test";

    private LocalStackContainer? _container;

    public string ServiceUrl => _container!.GetConnectionString();

    // Same image the AppHost runs
    public async ValueTask InitializeAsync()
        => _container = await TestSupport.StartContainerAsync(() => new LocalStackBuilder("localstack/localstack:4.4.0")
            .WithEnvironment("SERVICES", "sqs")
            .Build());

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
            await _container.DisposeAsync();
    }

    // ServiceURL alone (no RegionEndpoint): setting RegionEndpoint afterwards would override the endpoint.
    public AmazonSQSClient CreateClient() => new(
        new BasicAWSCredentials(AccessKey, SecretKey),
        new AmazonSQSConfig { ServiceURL = ServiceUrl, AuthenticationRegion = Region });
}
