using CommentsService.Communication;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommentsService.Models
{
    public class Comment
    {
        public long Id { get; set; }
        public string ? AuthorId { get; set; }
        public long ContentId { get; set; }
        public string ? Content { get; set; }
        public int ? Score { get; set; }
        public Comment(CommentDTO dto)
        {
            this.ContentId = dto.ContentId;
            this.Score = dto.Score;
            this.AuthorId = dto.AuthorId;
            this.Id = dto.Id;
            this.Content = dto.Content;
        }
        public Comment() { }
    }
}
