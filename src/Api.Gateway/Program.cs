using Api.Gateway;

var builder = WebApplication.CreateBuilder(args);

var gatewayOptions = builder.Configuration
    .GetSection(GatewayOptions.SectionName)
    .Get<GatewayOptions>()
    ?? throw new ApplicationException($"{nameof(GatewayOptions)} can't be build");

builder.Services.AddReverseProxyModule(gatewayOptions);
builder.Services.AddCompressionModule(gatewayOptions);

var app = builder.Build();

app.UseCompressionModule(gatewayOptions);

app.MapGet("/health", () => Results.Ok());

app.UseReverseProxyModule();

app.Run();

public partial class Program;