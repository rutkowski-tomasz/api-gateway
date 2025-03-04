var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/health", () => new
{
    Version = Environment.GetEnvironmentVariable("VERSION"),
    Environment = Environment.GetEnvironmentVariable("ENVIRONMENT"),
    HostName = Environment.GetEnvironmentVariable("HOSTNAME"),
    Environment.MachineName
});

app.MapReverseProxy();

app.Run();
