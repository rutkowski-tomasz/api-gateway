using Microsoft.Net.Http.Headers;
using Serilog;

namespace Api.Gateway;

internal static class SecurityHeadersModule
{
    public static void UseSecurityHeadersModule(this WebApplication app, GatewayOptions gatewayOptions)
    {
        if (!gatewayOptions.SecurityHeaders.Enabled)
        {
            return;
        }

        var contentSecurityPolicy = gatewayOptions.SecurityHeaders.ContentSecurityPolicy;
        Log.Information(
            "SecurityHeaders: Enabled with {ContentSecurityPolicy} content security policy",
            contentSecurityPolicy ?? "no"
        );

        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.TryAdd(HeaderNames.XContentTypeOptions, "nosniff");
                headers.TryAdd(HeaderNames.XFrameOptions, "DENY");
                headers.TryAdd("Referrer-Policy", "no-referrer");
                if (!string.IsNullOrEmpty(contentSecurityPolicy))
                {
                    headers.TryAdd(HeaderNames.ContentSecurityPolicy, contentSecurityPolicy);
                }

                return Task.CompletedTask;
            });

            await next();
        });
    }
}
