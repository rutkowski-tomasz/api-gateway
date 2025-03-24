using Yarp.ReverseProxy.Configuration;

namespace Api.Gateway;

internal static class ReverseProxyModule 
{
    public static void AddReverseProxyModule(this IServiceCollection services, GatewayOptions gatewayOptions)
    {
        var routes = gatewayOptions.Services.Select(x => new RouteConfig()
        {
            RouteId = $"{x.Name}-route",
            ClusterId = $"{x.Name}-cluster",
            Match = new RouteMatch
            {
                Path = $"{x.Name}/{{**catch-all}}"
            },
            Transforms =
            [
                new Dictionary<string, string>
                {
                    ["PathPattern"] = "{**catch-all}"
                }
            ]
        }).ToList();

        var clusters = gatewayOptions.Services.Select(x => new ClusterConfig()
        {
            ClusterId = $"{x.Name}-cluster",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                { "destination1", new DestinationConfig()
                    {
                        Address = $"http://{x.Name}"
                    }
                }
            }
        }).ToList();

        services
            .AddReverseProxy()
            .LoadFromMemory(routes, clusters);
    }

    public static void UseReverseProxyModule(this WebApplication app)
    {
        app.MapReverseProxy();
    }
}