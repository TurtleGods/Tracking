using System.Diagnostics.CodeAnalysis;

namespace Tracking.Api.Data;

[ExcludeFromCodeCoverage]
public sealed class ClickHouseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}
