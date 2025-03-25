using Microsoft.AspNetCore.RateLimiting;
using Serilog;

namespace Api.Gateway;

internal static class RateLimitingModule 
{

    public static void AddRateLimitingModule(this IServiceCollection services, GatewayOptions gatewayOptions)
    {
        var servicesWithRateLimiting = GetServicesWithRateLimiting(gatewayOptions);
        if (servicesWithRateLimiting.Count == 0)
        {
            return;
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            foreach (var service in servicesWithRateLimiting)
            {
                Log.Information(
                    "RateLimiting: {ServiceName} limit {PermitLimit} in {WindowSeconds}s window",
                    service.Name,
                    service.RateLimiting!.PermitLimit,
                    service.RateLimiting!.WindowSeconds
                );

                options.AddFixedWindowLimiter(BuildRateLimiterPolicyName(service)!, x => {
                    x.PermitLimit = service.RateLimiting!.PermitLimit;
                    x.Window = TimeSpan.FromSeconds(service.RateLimiting.WindowSeconds);
                });
            }
        });
    }

    public static void UseRateLimitingModule(this WebApplication app, GatewayOptions gatewayOptions)
    {
        var servicesWithRateLimiting = GetServicesWithRateLimiting(gatewayOptions);
        if (servicesWithRateLimiting.Count == 0)
        {
            return;
        }

        app.UseRateLimiter();
    }

    public static string? BuildRateLimiterPolicyName(ServiceOptions serviceOptions)
    {
        return IsRateLimitingEnabled(serviceOptions.RateLimiting)
            ? $"{serviceOptions.Name}-RateLimitPolicy"
            : null;
    }

    private static List<ServiceOptions> GetServicesWithRateLimiting(GatewayOptions gatewayOptions)
    {
        return [.. gatewayOptions.Services.Where(x => IsRateLimitingEnabled(x.RateLimiting))];
    }

    private static bool IsRateLimitingEnabled(RateLimitingOptions? rateLimitingOptions)
    {
        return rateLimitingOptions is not null
            && rateLimitingOptions.PermitLimit > 0
            && rateLimitingOptions.WindowSeconds > 0;
    }
}