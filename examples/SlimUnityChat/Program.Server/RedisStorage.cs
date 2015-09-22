using StackExchange.Redis;

namespace SlimUnityChat.Program.Server
{
    public class RedisStorage
    {
        public static RedisStorage Instance { get; set; }
        public static IDatabase Db { get { return Instance.GetDatabase(); } }

        private ConnectionMultiplexer _connection;

        public RedisStorage(string configuration)
        {
            _connection = ConnectionMultiplexer.Connect(configuration);
        }

        public IDatabase GetDatabase(int db = -1)
        {
            return _connection.GetDatabase(db);
        }
    }
}