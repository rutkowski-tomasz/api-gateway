using System.Net;
using System.Net.Http.Headers;

namespace Api.Gateway.IntegrationTests;

public class CachingTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Service_ShouldServeFromCache_WhenDownstreamAllowsCaching()
    {
        // Act
        var firstResponse = await factory.Client.GetAsync($"/{factory.Api1Name}/cacheable-random");
        var secondResponse = await factory.Client.GetAsync($"/{factory.Api1Name}/cacheable-random");

        // Assert
        var firstContent = await firstResponse.Content.ReadAsStringAsync();
        var secondContent = await secondResponse.Content.ReadAsStringAsync();
        secondContent.ShouldBe(firstContent);
        secondResponse.Headers.Age.ShouldNotBeNull();
    }

    [Fact]
    public async Task Service_ShouldNotCache_WhenDownstreamDoesNotAllowCaching()
    {
        // Act
        var firstResponse = await factory.Client.GetAsync($"/{factory.Api1Name}/random");
        var secondResponse = await factory.Client.GetAsync($"/{factory.Api1Name}/random");

        // Assert
        var firstContent = await firstResponse.Content.ReadAsStringAsync();
        var secondContent = await secondResponse.Content.ReadAsStringAsync();
        secondContent.ShouldNotBe(firstContent);
    }

    [Fact]
    public async Task Service_ShouldCacheSeparately_WhenQueryStringDiffers()
    {
        // Act
        var firstResponse = await factory.Client.GetAsync($"/{factory.Api1Name}/cacheable-random?page=1");
        var secondResponse = await factory.Client.GetAsync($"/{factory.Api1Name}/cacheable-random?page=2");

        // Assert
        var firstContent = await firstResponse.Content.ReadAsStringAsync();
        var secondContent = await secondResponse.Content.ReadAsStringAsync();
        secondContent.ShouldNotBe(firstContent);
        secondResponse.Headers.Age.ShouldBeNull();
    }

    [Fact]
    public async Task Service_ShouldNotCache_WhenQueryContainsCacheKeyDelimiter()
    {
        // Arrange
        var path = $"/{factory.Api1Name}/cacheable-random?a=x%1EB=y";
        await factory.Client.GetAsync(path);

        // Act
        var response = await factory.Client.GetAsync(path);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Age.ShouldBeNull();
    }

    [Fact]
    public async Task Service_ShouldNotShareCacheEntry_WhenQueryKeyContainsEqualsSign()
    {
        // Act
        var attackerContent = await factory.Client.GetStringAsync($"/{factory.Api1Name}/cacheable-random?a%3D1=2");
        var victimResponse = await factory.Client.GetAsync($"/{factory.Api1Name}/cacheable-random?a=1%3D2");

        // Assert
        var victimContent = await victimResponse.Content.ReadAsStringAsync();
        victimContent.ShouldNotBe(attackerContent);
        victimResponse.Headers.Age.ShouldBeNull();
    }

    [Fact]
    public async Task Service_ShouldNotShareCacheEntry_WhenQueryContainsInvalidEscape()
    {
        // Act
        var attackerContent = await factory.Client.GetStringAsync($"/{factory.Api1Name}/cacheable-random?a=%25FF");
        var victimResponse = await factory.Client.GetAsync($"/{factory.Api1Name}/cacheable-random?a=%FF");

        // Assert
        var victimContent = await victimResponse.Content.ReadAsStringAsync();
        victimContent.ShouldNotBe(attackerContent);
        victimResponse.Headers.Age.ShouldBeNull();
    }

    [Fact]
    public async Task Service_ShouldNotServeFromCache_WhenRequestIsAuthorized()
    {
        // Arrange
        var path = $"/{factory.Api1Name}/cacheable-random?authorized";
        var cachedContent = await factory.Client.GetStringAsync(path);
        var cachedResponse = await factory.Client.GetAsync(path);
        cachedResponse.Headers.Age.ShouldNotBeNull();

        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token");

        // Act
        var response = await factory.Client.SendAsync(request);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBe(cachedContent);
        response.Headers.Age.ShouldBeNull();
    }
}
