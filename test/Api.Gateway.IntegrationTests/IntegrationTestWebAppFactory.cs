using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using System.Net;

namespace Api.Gateway.IntegrationTests;

public class IntegrationTestWebAppFactory : IAsyncLifetime
{
    private INetwork? network;
    private IContainer? apiContainer;
    private IFutureDockerImage? gatewayImage;
    private IContainer? gatewayContainer;
    private readonly string mappingDirectory;
    public HttpClient HttpClient { get; private set; } = new();

    public IntegrationTestWebAppFactory()
    {
        mappingDirectory = Path.Combine(Path.GetTempPath(), $"wiremock-mappings");
        Directory.CreateDirectory(mappingDirectory);

        File.WriteAllText($"{mappingDirectory}/{Guid.NewGuid():N}.json", """
        {
            "request": { "method": "GET", "url": "/metadata" },
            "response": {
                "status": 200,
                "jsonBody": "api1",
                "headers": { "Content-Type": "application/json" }
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
                {"ASPNETCORE_URLS", "http://+:80"},
                {"Logging__LogLevel__Default", "Debug"},
                {"Logging__LogLevel__Microsoft.AspNetCore.HttpLogging", "Information"},
                {"Logging__LogLevel__Yarp", "Debug"},
                {"ReverseProxy__Routes__api1-route__ClusterId", "api1-cluster"},
                {"ReverseProxy__Routes__api1-route__Match__Path", "api1/{**catch-all}"},
                {"ReverseProxy__Routes__api1-route__Transforms__0__PathPattern", "{**catch-all}"},
                {"ReverseProxy__Clusters__api1-cluster__Destinations__destination1__Address", "http://api1:8080"}
            })
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .Build();

        await gatewayContainer.StartAsync();

        var gatewayPort = gatewayContainer.GetMappedPublicPort(80);
        HttpClient = new HttpClient
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
        HttpClient.Dispose();
    }
}
