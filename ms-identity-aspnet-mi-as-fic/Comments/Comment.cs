namespace MiFicExamples.Comments;

public record Comment
{
    public string? Name { get; init; }

    public string? Text { get; set; }
}
