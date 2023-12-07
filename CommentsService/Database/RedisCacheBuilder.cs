using StackExchange.Redis;

namespace CommentsService.Database
{
    public class RedisCacheBuilder
    {
        private static ConnectionMultiplexer redis;
        private const string HOSTNAME = "host.docker.internal";
        private const int PORT = 6479;

        public static IDatabase GetDatabase()
        {
            try
            {
                redis ??= ConnectionMultiplexer.Connect(
                        new ConfigurationOptions
                        {
                            EndPoints = { $"{HOSTNAME}:{PORT}" },
                        }
                    );

                PingDatabse();

                return redis.GetDatabase();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to initialize redis connection!\n{exception.Message}");
                throw exception;
            }
        }
        private static void PingDatabse()
        {

            var Pong = redis.GetDatabase().Ping();
            Console.WriteLine($"Ping response received in {Pong}");
        }
    }
}
