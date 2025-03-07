using System.Net;

namespace Api.Gateway.IntegrationTests;

public class GatewayIntegrationTests(IntegrationTestWebAppFactory factory)
    : IClassFixture<IntegrationTestWebAppFactory>
{
    [Fact]
    public async Task Api1_Metadata_Returns_Expected_Version()
    {
        // Act
        var response = await factory.HttpClient.GetAsync("/api1/metadata");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe("api1");
    }
}
