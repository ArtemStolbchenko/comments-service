using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;
using CommentsService.Models;
using System.Collections.Concurrent;

namespace CommentsService.Communication
{
    public class RabbitMQManager : IRabbitMQManager
    {
        private IConnection _connection;
        private IModel _channel;
        private const string HOSTNAME = "host.docker.internal",
                             QUEUE = "comments";
        private const int PORT = 5772;
        private string _replyQueueName;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper = new();

        public RabbitMQManager()
        {
            _connection = Connect();
            StartListening();
        }
        private IConnection Connect(int Try = 0)
        {
            try
            {
                System.Console.WriteLine($"Trying to connect to {HOSTNAME}:{PORT}. Try #{Try + 1}");
                var factory = new ConnectionFactory { HostName = HOSTNAME, Port = PORT };
                IConnection connection = factory.CreateConnection();

                AppDomain.CurrentDomain.ProcessExit += DisposeConnection;
                return connection;
            }
            catch (BrokerUnreachableException exception)
            {
                Console.WriteLine("Connection failed");
                if (Try < 5)
                {
                    Thread.Sleep(1000);
                    return Connect(Try + 1);
                }
                else
                {
                    throw exception;
                }
            }
        }
        public async Task Send(Comment comment, CommentAction action)
        {
            CommentDTO commentDTO = new CommentDTO(comment);
            commentDTO.Action = action;
            await Send(commentDTO);
        }
        public async Task SendDelete(long id)
        {
            CommentDTO commentDTO = new CommentDTO() { Id = id, Action = CommentAction.Delete };
            await Send(commentDTO);
        }
        private async Task Send(object body)
        {
            Send(GetBytes(body));
        }

        public Task<string> RequestUpdate(CancellationToken cancellationToken = default)
        {
            CommentDTO commentDTO = new CommentDTO() { Action = CommentAction.Read };
            string json = JsonConvert.SerializeObject(commentDTO);

            IBasicProperties props = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = _replyQueueName;
            var messageBytes = Encoding.UTF8.GetBytes(json);
            var tcs = new TaskCompletionSource<string>();
            _callbackMapper.TryAdd(correlationId, tcs);
            Console.WriteLine($"RabbitMQ: Sending {messageBytes}");
            _channel.BasicPublish(exchange: string.Empty,
                                 routingKey: QUEUE,
                                 basicProperties: props,
                                 body: messageBytes);

            cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out _));
            return tcs.Task;
        }

        private void StartListening()
        {
            _channel = _connection.CreateModel();

            _replyQueueName = _channel.QueueDeclare().QueueName;
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                Console.WriteLine($"RabbitMQ: A response is received");
                if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                    return;
                var body = ea.Body.ToArray();
                var response = Encoding.UTF8.GetString(body);
                tcs.TrySetResult(response);
            };

            _channel.BasicConsume(consumer: consumer,
                                 queue: _replyQueueName,
                                 autoAck: true);
        }

        private byte[] GetBytes(object sourceObject)
        {
            string json = JsonConvert.SerializeObject(sourceObject);
            var bytes = Encoding.UTF8.GetBytes(json);
            return bytes;
        }
        private void Send(byte[] message)
        {
            using var channel = _connection.CreateModel();
            {
                channel.BasicPublish(exchange: string.Empty,
                                     routingKey: QUEUE,
                                     basicProperties: null,
                                     body: message);
                Console.WriteLine($"[x] Sent {message}\n to {QUEUE}");
            }

        }
        private void DisposeConnection(object? sender, EventArgs? e)
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}
