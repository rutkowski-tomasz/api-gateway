using System.Net;

namespace Api.Gateway.IntegrationTests;

public class SecurityHeadersTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Service_ShouldIncludeSecurityHeaders()
    {
        // Act
        var response = await factory.Client.GetAsync($"/{factory.Api1Name}/echo");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        response.Headers.GetValues("X-Frame-Options").ShouldBe(["DENY"]);
        response.Headers.GetValues("Referrer-Policy").ShouldBe(["no-referrer"]);
        response.Headers.GetValues("Content-Security-Policy").ShouldBe(["default-src 'none'"]);
    }

    [Fact]
    public async Task Service_ShouldKeepSecurityHeader_WhenSetByDownstream()
    {
        // Act
        var response = await factory.Client.GetAsync($"/{factory.Api1Name}/framed");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("X-Frame-Options").ShouldBe(["SAMEORIGIN"]);
    }

    [Fact]
    public async Task GatewayResponse_ShouldIncludeSecurityHeaders()
    {
        // Act
        var response = await factory.Client.GetAsync("/non-existing");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
    }
}
