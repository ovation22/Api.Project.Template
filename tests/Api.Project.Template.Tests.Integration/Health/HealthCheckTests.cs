using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace Api.Project.Template.Tests.Integration.Health;

public class HealthCheckTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    [Trait("Category", "Health")]
    public async Task Ready_InDevelopment_ReturnsDetailsIncludingTheDatabaseCheck()
    {
        // Arrange
        // The database check is registered by Aspire's AddSqlServerDbContext (named after the DbContext);
        // the template no longer adds a second one of its own.
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz/ready", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ApiProjectTemplateContext", body);
    }

    [Fact]
    [Trait("Category", "Health")]
    public async Task Ready_OutsideDevelopment_ReturnsOnlyTheStatus()
    {
        // Arrange
        // Check names, durations and exception messages must not be public in production.
        var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production")).CreateClient();

        // Act
        var response = await client.GetAsync("/healthz/ready", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }
}
