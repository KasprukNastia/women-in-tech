namespace MiFicExamples.Storage;

public record Blob
{
    public required string Name { get; init; }
    public required string Content { get; init; }
}