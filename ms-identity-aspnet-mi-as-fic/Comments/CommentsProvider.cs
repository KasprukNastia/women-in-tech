using MiFicExamples.Storage;
using MiFicExamples.Storage.Configuration;

namespace MiFicExamples.Comments;

public class CommentsProvider : ICommentsProvider
{
    private readonly BlobStorageConfig _blobStorageConfig;
    private readonly IBlobStorageClient _blobStorageClient;

    public CommentsProvider(
        BlobStorageConfig blobStorageConfig,
        IBlobStorageClient blobStorageClient)
    {
        _blobStorageConfig = blobStorageConfig;
        _blobStorageClient = blobStorageClient;
    }

    public async Task<List<Comment>> GetAllComments()
    {
        var blobs = await _blobStorageClient.GetAllBlobsFromStorage(_blobStorageConfig);

        var comments = new List<Comment>(blobs.Count);
        foreach (var blob in blobs)
        {
            comments.Add(new Comment { Name = blob.Name, Text = blob.Content });
        }

        return comments;
    }

    public async Task CreateComment(Comment comment)
    {
        await _blobStorageClient.UploadBlobToStorage(_blobStorageConfig, new Blob { Name = comment.Name, Content = comment.Text });
    }

    public async Task DeleteComment(Comment comment)
    {
        await _blobStorageClient.DeleteBlobFromStorage(_blobStorageConfig, comment.Name);
    }
}
