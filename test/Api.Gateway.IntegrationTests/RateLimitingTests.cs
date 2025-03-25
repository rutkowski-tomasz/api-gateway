using System.Net;

namespace Api.Gateway.IntegrationTests;

public class RateLimitingTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Service_ShouldNotCompress_WhenGzipNotRequested()
    {
        // Arrange
        const int expectedPermitLimit = 10;

        // Act
        var tasks = Enumerable.Range(0, expectedPermitLimit + 1).Select(_ =>
            factory.Client.GetAsync($"/{factory.Api2Name}/echo")
        );

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Count(x => x.StatusCode == HttpStatusCode.TooManyRequests).ShouldBe(1);
    }
}
