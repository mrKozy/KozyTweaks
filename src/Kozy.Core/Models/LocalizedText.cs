namespace Kozy.Core.Models;

public sealed record LocalizedText
{
    public required string De { get; init; }
    public string? En { get; init; }
}