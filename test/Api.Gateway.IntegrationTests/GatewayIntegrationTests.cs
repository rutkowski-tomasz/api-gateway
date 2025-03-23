using System.Net;
using System.IO.Compression;

namespace Api.Gateway.IntegrationTests;

public class GatewayIntegrationTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task HealthEndpoint_ShouldReturnOk()
    {
        // Act
        var response = await factory.Client.GetAsync("/health");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NonExistingEndpoint_ShouldReturnNotFound()
    {
        // Act
        var response = await factory.Client.GetAsync("/non-existing");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownStreamApi_ShouldReturnOk()
    {
        // Act
        var response = await factory.Client.GetAsync("/integration-tests-api1/echo");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe("Hello world!");
    }

    [Fact]
    public async Task DownStreamApi_ShouldReturnNotFound()
    {
        // Act
        var response = await factory.Client.GetAsync("/integration-tests-api1/non-existing");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownStreamApi_ShouldReturnCustomHeader()
    {
        // Act
        var response = await factory.Client.GetAsync("/integration-tests-api1/echo");

        // Assert
        response.Headers.GetValues("X-Custom-Header").ShouldBe(["custom-header"]);
    }

    [Fact]
    public async Task DownStreamApi_ShouldReturnPassthroughHeaderFromRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/integration-tests-api1/echo");
        request.Headers.Add("X-Custom-Header", "my-overriden-custom-header");

        // Act
        var response = await factory.Client.SendAsync(request);

        // Assert
        response.Headers.GetValues("X-Custom-Header").ShouldBe(["my-overriden-custom-header"]);
    }
    
    [Fact]
    public async Task DownStreamApi_ShouldReturnServiceName()
    {
        // Act
        var response = await factory.Client.GetAsync("/integration-tests-api1/echo");

        // Assert
        response.Headers.GetValues("X-Service-Name").ShouldBe(["api1"]);
    }
}
