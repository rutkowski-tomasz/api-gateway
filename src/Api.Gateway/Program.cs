
// using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddResponseCompression(options =>
// {
//     options.EnableForHttps = true;
//     options.Providers.Add<GzipCompressionProvider>();
// });

// builder.Services.Configure<GzipCompressionProviderOptions>(options =>
//     options.Level = System.IO.Compression.CompressionLevel.Fastest
// );

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());

app.MapReverseProxy();

app.Run();

public partial class Program;