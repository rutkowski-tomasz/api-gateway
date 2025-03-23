using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using System.Net;

namespace Api.Gateway.IntegrationTests;

public class IntegrationTestFactory : IAsyncLifetime
{
    private INetwork? network;
    private IContainer? apiContainer;
    private IFutureDockerImage? gatewayImage;
    private IContainer? gatewayContainer;
    private readonly string mappingDirectory;
    public HttpClient Client { get; private set; } = new();

    public IntegrationTestFactory()
    {
        mappingDirectory = Path.Combine(Path.GetTempPath(), "wiremock-mappings");
        Directory.CreateDirectory(mappingDirectory);

        File.WriteAllText($"{mappingDirectory}/{Guid.NewGuid():N}.json", """
        {
            "request": { "method": "GET", "url": "/echo" },
            "response": {
                "status": 200,
                "body": "api1",
                "headers": {
                    "Content-Type": "text/plain",
                    "X-Custom-Header": "custom-header",
                    "X-Custom-Passthrough-Header": "{{request.headers.X-Custom-Passthrough-Header}}"
                }
            }
        }
        """);
    }

    public async Task InitializeAsync()
    {
        network = new NetworkBuilder()
            .WithName($"integration-tests-network")
            .WithDriver(NetworkDriver.Bridge)
            .Build();

        await network.CreateAsync();

        apiContainer = new ContainerBuilder()
            .WithImage("wiremock/wiremock:latest")
            .WithName($"wiremock-api1-{Guid.NewGuid():N}")
            .WithPortBinding(8080, true)
            .WithBindMount(mappingDirectory, "/home/wiremock/mappings")
            .WithCommand("--verbose", "--global-response-templating")
            .WithNetwork(network)
            .WithNetworkAliases("api1")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => 
                r.ForPort(8080).ForPath("/__admin/mappings").ForStatusCode(HttpStatusCode.OK)))
            .Build();

        await apiContainer.StartAsync();

        gatewayImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
            .WithDockerfile("Dockerfile")
            .Build();

        await gatewayImage.CreateAsync();

        gatewayContainer = new ContainerBuilder()
            .WithImage(gatewayImage)
            .WithName($"api-gateway-{Guid.NewGuid():N}")
            .WithPortBinding(80, true)
            .WithEnvironment(new Dictionary<string, string>
            {
                {"ASPNETCORE_ENVIRONMENT", "Production"},
                {"ReverseProxy__Routes__X1__ClusterId", "X1"},
                {"ReverseProxy__Routes__X1__Match__Path", "api3/{**catch-all}"},
                {"ReverseProxy__Routes__X1__Transforms__0__PathPattern", "{**catch-all}"},
                {"ReverseProxy__Clusters__X1__Destinations__destination1__Address", "http://api1:8080"},
            })
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .Build();

        await gatewayContainer.StartAsync();

        var gatewayPort = gatewayContainer.GetMappedPublicPort(80);
        Client = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{gatewayPort}")
        };
    }

    public async Task DisposeAsync()
    {
        await (gatewayContainer?.DisposeAsync() ?? ValueTask.CompletedTask);
        await (gatewayImage?.DisposeAsync() ?? ValueTask.CompletedTask);
        await (apiContainer?.DisposeAsync() ?? ValueTask.CompletedTask);
        await (network?.DisposeAsync() ?? ValueTask.CompletedTask);
        Directory.Delete(mappingDirectory, true);
        Client.Dispose();
    }
}
