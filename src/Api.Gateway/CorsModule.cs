
namespace Api.Gateway;

internal static class CorsModule 
{
    public static void AddCorsModule(this IServiceCollection services, GatewayOptions gatewayOptions)
    {
        var servicesWithCorsConfigured = GetServicesWithCorsConfigured(gatewayOptions);
        if (servicesWithCorsConfigured.Count == 0)
        {
            return;
        }

        services.AddCors(options =>
        {
            foreach (var service in servicesWithCorsConfigured)
            {
                options.AddPolicy(BuildCorsPolicyName(service)!, policy => policy
                    .WithOrigins(service.Cors!.Origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                );
            }
        });
    }

    public static void UseCorsModule(this WebApplication app, GatewayOptions gatewayOptions)
    {
        var servicesWithCorsConfigured = GetServicesWithCorsConfigured(gatewayOptions);
        if (servicesWithCorsConfigured.Count == 0)
        {
            return;
        }

        app.UseCors();
    }

    public static string? BuildCorsPolicyName(ServiceOptions serviceOptions)
    {
        return IsCorsConfigured(serviceOptions.Cors)
            ? $"{serviceOptions.Name}-CorsPolicy"
            : null;
    }

    private static List<ServiceOptions> GetServicesWithCorsConfigured(GatewayOptions gatewayOptions)
    {
        return [.. gatewayOptions.Services.Where(x => IsCorsConfigured(x.Cors))];
    }

    private static bool IsCorsConfigured(CorsOptions? corsOptions)
    {
        return corsOptions is not null
            && corsOptions.Origins.Any();
    }
}