using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;

namespace Api.Gateway.IntegrationTests;

public class IntegrationTestFactory : IAsyncLifetime
{
    private INetwork? network;
    private IContainer? api1Container;
    private IFutureDockerImage? gatewayImage;
    private IContainer? gatewayContainer;
    private readonly string id = Guid.NewGuid().ToString("N");

    public HttpClient Client { get; private set; }
    public string Api1Name { get; private set; }

    public async Task InitializeAsync()
    {
        network = new NetworkBuilder()
            .WithName($"integration-tests-network-{id}")
            .WithDriver(NetworkDriver.Bridge)
            .Build();

        await network.CreateAsync();

        var projectDir = CommonDirectoryPath.GetSolutionDirectory().DirectoryPath;
        var wiremockMappingsDir = Path.Combine(projectDir, "wiremock");

        Api1Name = $"integration-tests-api1-{id}";
        api1Container = new ContainerBuilder()
            .WithImage("wiremock/wiremock:latest")
            .WithName(Api1Name)
            .WithEnvironment(new Dictionary<string, string> { { "wiremock.service_name", "api1" } })
            .WithBindMount(wiremockMappingsDir, "/home/wiremock")
            .WithCommand("--port", "80", "--verbose", "--global-response-templating")
            .WithNetwork(network)
            .WithNetworkAliases(Api1Name)
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
            .WithName($"integration-tests-api-gateway-{id}")
            .WithPortBinding(80, true)
            .WithEnvironment(new Dictionary<string, string>
            {
                {"ASPNETCORE_ENVIRONMENT", "Production"},
                {"Gateway__Compression__Level", "Fastest"},
                {"Gateway__Services__0__Name", Api1Name },
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