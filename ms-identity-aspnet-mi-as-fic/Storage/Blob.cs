namespace MiFicExamples.Storage;

public record Blob
{
    public string? Key { get; init; }
    public string? Content { get; init; }
}