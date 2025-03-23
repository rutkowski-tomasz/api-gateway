using System.Net;
using System.IO.Compression;

namespace Api.Gateway.IntegrationTests;

public class CompressionTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Service_ShouldSupportGzipCompression()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/integration-tests-api1/echo");
        request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));

        // Act
        var response = await factory.Client.SendAsync(request);

        var compressedBytes = await response.Content.ReadAsByteArrayAsync();
        using var compressedStream = new MemoryStream(compressedBytes);
        using var decompressStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();
        await decompressStream.CopyToAsync(resultStream);
        resultStream.Position = 0;
        using var reader = new StreamReader(resultStream);
        var decompressedContent = await reader.ReadToEndAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentEncoding.ShouldContain("gzip");
        decompressedContent.ShouldBe("Hello world!");
    }

    [Fact]
    public async Task Service_ShouldNotCompress_WhenGzipNotRequested()
    {
        // Act
        var response = await factory.Client.GetAsync("/integration-tests-api1/echo");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentEncoding.ShouldNotContain("gzip");
    }
}
