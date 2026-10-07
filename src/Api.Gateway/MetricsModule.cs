using OpenTelemetry.Metrics;
using Serilog;

namespace Api.Gateway;

internal static class MetricsModule
{
    public const string MetricsPath = "/metrics";

    public static void AddMetricsModule(this IServiceCollection services, GatewayOptions gatewayOptions)
    {
        if (!gatewayOptions.Metrics.Enabled)
        {
            return;
        }

        Log.Information("Metrics: Enabled, Prometheus format at {MetricsPath}", MetricsPath);

        services
            .AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddPrometheusExporter()
            );
    }

    public static void UseMetricsModule(this WebApplication app, GatewayOptions gatewayOptions)
    {
        if (!gatewayOptions.Metrics.Enabled)
        {
            return;
        }

        app.MapPrometheusScrapingEndpoint(MetricsPath);
    }
}
