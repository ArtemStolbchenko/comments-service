using CommentsService.Models;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CommentsService.Database
{
    public interface IDatabaseHelper
    {
        public List<Comment> GetAll();
        public Comment GetById(long id);
        /// <summary>
        /// Replaces the stored list of comments with a new one
        /// </summary>
        /// <param name="Comments"></param>
        public void SaveAll(List<Comment> Comments);
        public bool Save(Comment Comment);
        public bool Delete(long id);
        public bool Update(Comment comment);
        public bool VerifyOwnership(long commentId, string userId);
    }

}
