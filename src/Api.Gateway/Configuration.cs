using System.IO.Compression;

namespace Api.Gateway;

public record GatewayOptions
{
    public const string SectionName = "Gateway";

    public CompressionOptions Compression { get; init; }
    public List<ServiceOptions> Services { get; init; }
}

public record CompressionOptions
{
    public CompressionLevel Level { get; init; } = CompressionLevel.NoCompression;
}

public record ServiceOptions
{
    public string Name { get; init; }
    public string? Prefix { get; init; }
    public RateLimitingOptions? RateLimiting { get; init; }
    public CorsOptions? Cors { get; init; }
}

public record RateLimitingOptions
{
    public int PermitLimit { get; init; }
    public int WindowSeconds { get; init; }
}

public record CorsOptions
{
    public string[] Origins { get; init; }
}