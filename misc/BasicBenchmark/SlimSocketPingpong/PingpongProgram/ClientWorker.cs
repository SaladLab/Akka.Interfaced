using System;
using System.Threading.Tasks;
using PingpongInterface;

namespace PingpongProgram.Client
{
    public class ClientWorker
    {
        private readonly ServerRef _server;

        public ClientWorker(ServerRef server)
        {
            _server = server;
        }

        public async Task Start(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var ret = await _server.Echo(i);
                if (i != ret)
                    throw new InvalidOperationException($"Wrong response (Expected:{i}, Actual:{ret})");
            }
        }
    }
}
