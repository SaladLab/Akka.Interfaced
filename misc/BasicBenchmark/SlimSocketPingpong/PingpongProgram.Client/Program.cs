using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Akka.Interfaced.SlimSocket.Base;
using Akka.Interfaced.SlimSocket.Client;
using Common.Logging;
using PingpongInterface;
using ProtoBuf.Meta;
using TypeAlias;

namespace PingpongProgram.Client
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            DoRemoteTest(100, 100);
        }

        private static void DoRemoteTest(int actorCount, int pingCount)
        {
            // make clients and servers

            var t1 = new Stopwatch();
            t1.Start();

            var clients = CreateRemoteClients(actorCount);

            var warmUpTasks = clients.Select(x => Task.Run(() => x.Start(1))).ToArray();
            Task.WaitAll(warmUpTasks);

            t1.Stop();
            Console.WriteLine($"Ready: {t1.Elapsed}");

            // test

            var t2 = new Stopwatch();
            t2.Start();

            var pingTasks = clients.Select(x => x.Start(pingCount)).ToArray();
            Task.WaitAll(pingTasks);

            t2.Stop();
            Console.WriteLine($"Test: {t2.Elapsed}");
        }

        private static ClientWorker[] CreateRemoteClients(int count)
        {
            var serializer = new PacketSerializer(
                new PacketSerializerBase.Data(
                    new ProtoBufMessageSerializer(TypeModel.Create()),
                    new TypeAliasTable()));

            var communicators = new Communicator[count];
            for (int i = 0; i < count; i++)
            {
                var communicator = new Communicator(LogManager.GetLogger("Communicator"),
                                                    new IPEndPoint(IPAddress.Loopback, 8081),
                                                    _ =>
                                                    new TcpConnection(serializer, LogManager.GetLogger("Connection")));
                communicator.Start();
                communicators[i] = communicator;
            }

            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(10);

                var connectedCount = communicators.Count(c => c.State == Communicator.StateType.Connected);
                if (connectedCount == count)
                    break;

                var closedCount = communicators.Count(c => c.State == Communicator.StateType.Stopped);
                if (closedCount > 0)
                    throw new Exception("Connection closed!");
            }

            var clients = new ClientWorker[count];
            for (int i = 0; i < count; i++)
            {
                var requestWaiter = new SlimTaskRequestWaiter(communicators[i]);
                var server = new ServerRef(new SlimActorRef(1), requestWaiter, null);
                clients[i] = new ClientWorker(server);
            }
            return clients;
        }
    }
}
