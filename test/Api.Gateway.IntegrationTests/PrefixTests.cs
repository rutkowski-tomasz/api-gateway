using System.Net;
using System.IO.Compression;

namespace Api.Gateway.IntegrationTests;

public class PrefixTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Service_ShouldNotAppendPrefix_WhenPrefixIsEmpty()
    {
        // Act
        var response = await factory.Client.GetAsync($"/{factory.Api1Name}/echo");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("X-Access").ShouldBe(["private"]);
    }

    [Fact]
    public async Task Service_ShouldAppendPrefix_WhenPrefixIsNotEmpty()
    {
        // Act
        var response = await factory.Client.GetAsync($"/{factory.Api2Name}/echo");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("X-Access").ShouldBe(["public"]);
    }

    [Fact]
    public async Task Service_ShouldNotScaffoldService()
    {
        // Act
        var response = await factory.Client.GetAsync($"/{factory.Api2Name}/../echo");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
