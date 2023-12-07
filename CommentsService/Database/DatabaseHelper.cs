using CommentsService.Models;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CommentsService.Database
{

    public class DatabaseHelper : IDatabaseHelper
    {
        private readonly IDatabase _database;
        private const string COMMENTS_KEY = "comments";
        public DatabaseHelper()
        {
            _database = RedisCacheBuilder.GetDatabase();
        }
        public List<Comment> GetAll()
        {
            var commentsList = _database.ListRange(COMMENTS_KEY);
            List<Comment> comments = commentsList.Select(
                serializedObject => System.Text.Json.JsonSerializer.Deserialize<Comment>(serializedObject)
                ).ToList();

            return comments;
        }
        public Comment GetById(long id)
        {
            Comment comment = JsonConvert.DeserializeObject<Comment>(_database.ListGetByIndex(COMMENTS_KEY, id));
            return comment;
        }
        /// <summary>
        /// Replaces the stored list of comments with a new one
        /// </summary>
        /// <param name="Comments"></param>
        public void SaveAll(List<Comment> Comments)
        {
            _database.ListTrim(COMMENTS_KEY, 1, 0); //Important! Removes the entire list before rewriting
            foreach (var comment in Comments)
            {
                Save(comment);
            }
        }
        public bool Save(Comment Comment)
        {
            try
            {
                Comment.Id = _database.ListLength(COMMENTS_KEY);

                string serializedComment = JsonConvert.SerializeObject(Comment);
                _database.ListRightPush(COMMENTS_KEY, serializedComment);

                return true;
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public bool Delete (long id)
        {
            try
            {
                var value = _database.ListGetByIndex(COMMENTS_KEY, id);
                _database.ListRemove(COMMENTS_KEY, value); 
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public bool Update(Comment comment)
        {
            try
            {
                string serializedComment = JsonConvert.SerializeObject(comment);
                _database.ListSetByIndex(COMMENTS_KEY, comment.Id, serializedComment);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public TimeSpan Ping()
        {
            return _database.Ping();
        }
        public bool VerifyOwnership(long commentId, string userId)
        {
            var comment = this.GetById(commentId);
            return comment.AuthorId == userId;
        }
    }

}
