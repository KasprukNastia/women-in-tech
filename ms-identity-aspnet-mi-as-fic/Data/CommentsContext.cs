using System.Collections.Generic;
using System.Threading.Tasks;
using MiFicExamples.Helpers;
using MiFicExamples.Models;
using Microsoft.Extensions.Options;

namespace MiFicExamples.Data
{
    public class CommentsContext
    {
        // make sure that appsettings.json is filled with the necessary details of the azure storage
        private readonly AzureStorageConfig _azureStorageConfig;
        private readonly IConfiguration _entraIdConfig;

        public CommentsContext(IOptions<AzureStorageConfig> azureStorageConfig, IConfiguration entraIdConfig)
        {
            _azureStorageConfig = azureStorageConfig.Value;
            _entraIdConfig = entraIdConfig;
        }

        public List<Comment>? Comments { get; set; }

        public async Task<List<Comment>> GetComments()
        {
            List<CommentBlobDTO> blobs = await StorageHelper.GetBlobs(_azureStorageConfig, _entraIdConfig);

            List<Comment> comments = new List<Comment>();
            foreach (CommentBlobDTO blob in blobs)
            {
                Comment comment = new Comment();
                comment.Name = blob.Name;
                comment.UserComment = blob.Contents;

                comments.Add(comment);
            }

            return comments;
        }

        public async Task CreateComment(Comment comment)
        {
            await StorageHelper.UploadBlob(_azureStorageConfig, _entraIdConfig, comment.Name, comment.UserComment);
        }

        public async Task DeleteComment(Comment comment)
        {

            await StorageHelper.DeleteBlob(_azureStorageConfig, _entraIdConfig, comment.Name);
        }
    }
}
