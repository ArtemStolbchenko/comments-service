using CommentsService.Models;

namespace CommentsService.Communication
{
    public class CommentDTO : Comment
    {
        public CommentAction Action { get; set; }
        public CommentDTO(Comment comment)
        {
            this.Content = comment.Content;
            this.AuthorId = comment.AuthorId;
            this.Score = comment.Score;
            this.Id = comment.Id;
        }

        public CommentDTO()
        {

        }
    }
}
