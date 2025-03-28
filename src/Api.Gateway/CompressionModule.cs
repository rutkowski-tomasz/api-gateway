using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;

namespace Api.Gateway;

internal static class CompressionModule 
{
    public static void AddCompressionModule(this IServiceCollection services, GatewayOptions gatewayOptions)
    {
        if (gatewayOptions.Compression.Level == CompressionLevel.NoCompression)
        {
            return;
        }

        Log.Information(
            "Compression: Enabled with {Level} level",
            gatewayOptions.Compression.Level
        );

        services
            .AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();
            })
            .Configure<GzipCompressionProviderOptions>(options =>
                options.Level = gatewayOptions.Compression.Level
            );
    }

    public static void UseCompressionModule(this WebApplication app, GatewayOptions gatewayOptions)
    {
        if (gatewayOptions.Compression.Level == CompressionLevel.NoCompression)
        {
            return;
        }

        app.UseResponseCompression();
    }
}