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
    private IContainer? api2Container;
    private IFutureDockerImage? gatewayImage;
    private IContainer? gatewayContainer;
    private readonly string id = Guid.NewGuid().ToString("N");

    public HttpClient Client { get; private set; }
    public string Api1Name { get; private set; }
    public string Api2Name { get; private set; }

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
            .WithCommand(
                "--port=80",
                "--verbose",
                "--global-response-templating",
                "--disable-banner"
            )
            .WithNetwork(network)
            .WithNetworkAliases(Api1Name)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .Build();

        await api1Container.StartAsync();

        Api2Name = $"integration-tests-api2-{id}";
        api2Container = new ContainerBuilder()
            .WithImage("wiremock/wiremock:latest")
            .WithName(Api2Name)
            .WithEnvironment(new Dictionary<string, string> { { "wiremock.service_name", "api2" } })
            .WithBindMount(wiremockMappingsDir, "/home/wiremock")
            .WithCommand(
                "--port=80",
                "--verbose",
                "--global-response-templating",
                "--disable-banner"
            )
            .WithNetwork(network)
            .WithNetworkAliases(Api2Name)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .Build();

        await api2Container.StartAsync();

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
                // Api1
                {"Gateway__Services__0__Name", Api1Name },
                // Api2
                {"Gateway__Services__1__Name", Api2Name },
                {"Gateway__Services__1__Prefix", "public"},
                {"Gateway__Services__1__RateLimiting__PermitLimit", "10"},
                {"Gateway__Services__1__RateLimiting__WindowSeconds", "60"},
                {"Gateway__Services__1__Cors__Origins__0", "http://localhost:3000"},
                {"Gateway__Services__1__Cors__Origins__1", "http://localhost:5000"},
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
        await (api2Container?.DisposeAsync() ?? ValueTask.CompletedTask);
        await (network?.DisposeAsync() ?? ValueTask.CompletedTask);
        Client.Dispose();
    }
}