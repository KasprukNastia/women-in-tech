namespace MiFicExamples.Comments;

public interface ICommentsProvider
{
    Task<List<Comment>> GetAllComments();
    Task CreateComment(Comment comment);
    Task DeleteComment(Comment comment);
}
