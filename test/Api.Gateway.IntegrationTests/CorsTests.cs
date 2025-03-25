using System.Net;

namespace Api.Gateway.IntegrationTests;

public class CorsTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Cors_AllowedOrigin_ShouldIncludeAllowOriginHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"/{factory.Api2Name}/echo");
        request.Headers.Add("Origin", "http://localhost:5000");

        // Act
        var response = await factory.Client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe(["http://localhost:5000"]);
    }

    [Fact]
    public async Task Cors_NoPolicy_ShouldNotIncludeAllowOriginHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"/{factory.Api1Name}/echo");
        request.Headers.Add("Origin", "http://localhost:3000");

        // Act
        var response = await factory.Client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }
}