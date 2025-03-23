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
    private IContainer? api1Container;
    private IFutureDockerImage? gatewayImage;
    private IContainer? gatewayContainer;
    public HttpClient Client { get; private set; } = new();

    public async Task InitializeAsync()
    {
        network = new NetworkBuilder()
            .WithName($"integration-tests-network")
            .WithDriver(NetworkDriver.Bridge)
            .Build();

        await network.CreateAsync();

        var projectDir = CommonDirectoryPath.GetSolutionDirectory().DirectoryPath;
        var wiremockMappingsDir = Path.Combine(projectDir, "wiremock");

        api1Container = new ContainerBuilder()
            .WithImage("wiremock/wiremock:latest")
            .WithName("integration-tests-api1")
            .WithEnvironment(new Dictionary<string, string> { { "wiremock.service_name", "api1" } })
            .WithBindMount(wiremockMappingsDir, "/home/wiremock")
            .WithCommand("--port", "80", "--verbose", "--global-response-templating")
            .WithNetwork(network)
            .WithNetworkAliases("integration-tests-api1")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .Build();

        await api1Container.StartAsync();

        gatewayImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
            .WithDockerfile("Dockerfile")
            .Build();

        await gatewayImage.CreateAsync();

        gatewayContainer = new ContainerBuilder()
            .WithImage(gatewayImage)
            .WithName($"integration-tests-api-gateway")
            .WithPortBinding(80, true)
            .WithEnvironment(new Dictionary<string, string>
            {
                {"ASPNETCORE_ENVIRONMENT", "Production"},
                {"ReverseProxy__Routes__X1__ClusterId", "X1"},
                {"ReverseProxy__Routes__X1__Match__Path", "integration-tests-api1/{**catch-all}"},
                {"ReverseProxy__Routes__X1__Transforms__0__PathPattern", "{**catch-all}"},
                {"ReverseProxy__Clusters__X1__Destinations__destination1__Address", "http://integration-tests-api1"},
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
        await (api1Container?.DisposeAsync() ?? ValueTask.CompletedTask);
        await (network?.DisposeAsync() ?? ValueTask.CompletedTask);
        Client.Dispose();
    }
}