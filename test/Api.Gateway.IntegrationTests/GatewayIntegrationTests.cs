using System.Net;

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
        var response = await factory.Client.GetAsync("/api3/echo");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe("api1");
    }

    [Fact]
    public async Task DownStreamApi_ShouldReturnNotFound()
    {
        // Act
        var response = await factory.Client.GetAsync("/api3/non-existing");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownStreamApi_ShouldReturnCustomHeader()
    {
        // Act
        var response = await factory.Client.GetAsync("/api3/echo");

        // Assert
        response.Headers.GetValues("X-Custom-Header").ShouldBe(["custom-header"]);
    }

    [Fact]
    public async Task DownStreamApi_ShouldReturnCustomPassthroughHeader()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api3/echo");
        request.Headers.Add("X-Custom-Passthrough-Header", "my-custom-header");
        var response = await factory.Client.SendAsync(request);

        // Assert
        response.Headers.GetValues("X-Custom-Passthrough-Header").ShouldBe(["my-custom-header"]);
    }
}
