using System.Net;

namespace Api.Gateway.IntegrationTests;

public class MetricsTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Metrics_ShouldIncludeRequestDurationPerServiceAndStatusCode()
    {
        // Arrange
        await factory.Client.GetAsync($"/{factory.Api1Name}/echo");
        await factory.Client.GetAsync($"/{factory.Api1Name}/not-found");

        // Act
        var response = await factory.Client.GetAsync("/metrics");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var serviceRoute = $"http_route=\"{factory.Api1Name}/{{**catch-all}}\"";
        content.ShouldContain($"http_server_request_duration_seconds_count{{otel_scope_name=\"Microsoft.AspNetCore.Hosting\",http_request_method=\"GET\",http_response_status_code=\"200\",{serviceRoute}");
        content.ShouldContain($"http_server_request_duration_seconds_count{{otel_scope_name=\"Microsoft.AspNetCore.Hosting\",http_request_method=\"GET\",http_response_status_code=\"404\",{serviceRoute}");
    }
}
