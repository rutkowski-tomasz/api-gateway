using Api.Gateway;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

var gatewayOptions = builder.Configuration
    .GetSection(GatewayOptions.SectionName)
    .Get<GatewayOptions>()
    ?? throw new ApplicationException($"{nameof(GatewayOptions)} can't be build");

const string healthPath = "/health";
builder.Services.AddSerilog(config => config
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Yarp.ReverseProxy", LogEventLevel.Warning)
    .WriteTo.Console()
    .Filter.ByExcluding(logEvent => 
        logEvent.Properties.TryGetValue("RequestPath", out var requestPath) && 
        requestPath.ToString().Contains(healthPath)
    )
);
builder.Services.AddReverseProxyModule(gatewayOptions);
builder.Services.AddCompressionModule(gatewayOptions);
builder.Services.AddRateLimitingModule(gatewayOptions);
builder.Services.AddCorsModule(gatewayOptions);

var app = builder.Build();

app.MapGet(healthPath, () => Results.Ok());

app.UseSerilogRequestLogging();
app.UseRateLimitingModule(gatewayOptions);
app.UseCorsModule(gatewayOptions);
app.UseCompressionModule(gatewayOptions);
app.UseReverseProxyModule();

app.Run();

public partial class Program;