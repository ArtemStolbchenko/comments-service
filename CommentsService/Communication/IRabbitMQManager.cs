using CommentsService.Models;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Channels;

namespace CommentsService.Communication
{
    public interface IRabbitMQManager
    {
        public Task Send(Comment comment, CommentAction action);
        public Task SendDelete(long id);
        public Task<string> RequestUpdate(CancellationToken cancellationToken = default);
    }
}
