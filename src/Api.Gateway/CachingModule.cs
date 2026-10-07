using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Net.Http.Headers;
using Serilog;
using Yarp.ReverseProxy.Forwarder;

namespace Api.Gateway;

internal static class CachingModule
{
    // Every cacheable response is buffered in memory while streaming, so the default 64 MB per request is too much for a gateway
    private const long MaximumCacheableBodySize = 1024 * 1024;

    public static void AddCachingModule(this IServiceCollection services, GatewayOptions gatewayOptions)
    {
        if (!gatewayOptions.Caching.Enabled)
        {
            return;
        }

        Log.Information("Caching: Enabled, following downstream Cache-Control headers");

        services.AddResponseCaching(options =>
        {
            options.UseCaseSensitivePaths = true;
            options.MaximumBodySize = MaximumCacheableBodySize;
        });
    }

    public static void UseCachingModule(this WebApplication app, GatewayOptions gatewayOptions)
    {
        if (!gatewayOptions.Caching.Enabled)
        {
            return;
        }

        app.UseWhen(context => !HasAmbiguousQuery(context.Request.Query), cachingApp =>
        {
            cachingApp.UseResponseCaching();
            cachingApp.Use(VaryByQuery);
            cachingApp.Use(SkipStoringTruncatedResponse);
        });
    }

    // The cache key is built from the decoded query, joining a key and its value with '='.
    // ?a%3D1=2 and ?a=1%3D2 decode to the same key, and so do ?a=%FF and ?a=%25FF (invalid escapes stay as text)
    private static bool HasAmbiguousQuery(IQueryCollection query)
    {
        return query.Any(parameter =>
            parameter.Key.Contains('=')
            || parameter.Key.Contains('%')
            || parameter.Value.Any(value => value!.Contains('%')));
    }

    // Without it the cache key ignores the query string and ?page=1 would be served for ?page=2
    private static Task VaryByQuery(HttpContext context, RequestDelegate next)
    {
        context.Features.Get<IResponseCachingFeature>()!.VaryByQueryKeys = ["*"];
        return next(context);
    }

    // A downstream failure mid-body leaves a truncated response that the cache would store; throwing skips storing it
    private static async Task SkipStoringTruncatedResponse(HttpContext context, RequestDelegate next)
    {
        await next(context);

        var forwarderError = context.Features.Get<IForwarderErrorFeature>();
        var isCacheable = HeaderUtilities.ContainsCacheDirective(context.Response.Headers.CacheControl, CacheControlHeaderValue.PublicString);
        if (context.Response.HasStarted && forwarderError is not null && isCacheable)
        {
            throw new InvalidOperationException($"Downstream response failed with {forwarderError.Error}", forwarderError.Exception);
        }
    }
}
