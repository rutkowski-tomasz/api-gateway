using Yarp.ReverseProxy.Configuration;

namespace Api.Gateway;

internal static class ReverseProxyModule 
{
    public static void AddReverseProxyModule(this IServiceCollection services, GatewayOptions gatewayOptions)
    {
        var routes = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();
        foreach (var service in gatewayOptions.Services)
        {
            var clusterId = $"{service.Name}-cluster";
            var prefix = !string.IsNullOrEmpty(service.Prefix) ? $"/{service.Prefix}" : string.Empty;
            var destinationAddress = $"http://{service.Name}{prefix}";
            var rateLimiterPolicy = RateLimitingModule.BuildRateLimiterPolicyName(service);
            var corsPolicy = CorsModule.BuildCorsPolicyName(service);

            routes.Add(new RouteConfig()
            {
                RouteId = $"{service.Name}-route",
                ClusterId = clusterId,
                RateLimiterPolicy = rateLimiterPolicy,
                CorsPolicy = corsPolicy,
                Match = new RouteMatch
                {
                    Path = $"{service.Name}/{{**catch-all}}"
                },
                Transforms =
                [
                    new Dictionary<string, string>
                    {
                        ["PathPattern"] = "{**catch-all}"
                    }
                ]
            });

            clusters.Add(new ClusterConfig()
            {
                ClusterId = clusterId,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    {
                        "destination1",
                        new DestinationConfig()
                        {
                            Address = destinationAddress
                        }
                    }
                }
            });
        }

        services
            .AddReverseProxy()
            .LoadFromMemory(routes, clusters);
    }

    public static void UseReverseProxyModule(this WebApplication app)
    {
        app.MapReverseProxy();
    }
}