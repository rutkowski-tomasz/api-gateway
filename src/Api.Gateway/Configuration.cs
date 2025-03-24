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
}
