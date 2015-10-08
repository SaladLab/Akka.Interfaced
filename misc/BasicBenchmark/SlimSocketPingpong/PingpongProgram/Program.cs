using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Interfaced;
using Akka.Interfaced.ProtobufSerializer;
using Akka.Interfaced.SlimSocketBase;
using Akka.Interfaced.SlimSocketClient;
using PingpongInterface;
using PingpongProgram.Client;
using PingpongProgram.ClientLib;
using PingpongProgram.Server;
using PingpongProgram.ServerLib;
using TypeAlias;

namespace PingpongProgram
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

            CreateRemoteServer();
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

        private static void CreateRemoteServer()
        {
            var system = ActorSystem.Create("ServerSystem");
            DeadRequestProcessingActor.Install(system);

            var gateway = system.ActorOf(Props.Create<ClientGateway>());
            gateway.Tell(new ClientGatewayMessage.Start {ServiceEndPoint = new IPEndPoint(IPAddress.Loopback, 8081)});
        }

        private static ClientWorker[] CreateRemoteClients(int count)
        {
            var serializer = new PacketSerializer(
                new PacketSerializerBase.Data(
                    new ProtoBufMessageSerializer(ProtobufSerializer.CreateTypeModel()),
                    new TypeAliasTable()));

            var connections = new TcpConnection[count];
            var connectedCount = 0;
            var closedCount = 0;
            for (int i = 0; i < count; i++)
            {
                var connection = new TcpConnection(serializer, null);
                connection.Connected += _ => { Interlocked.Increment(ref connectedCount); };
                connection.Closed += (_, __) => { Interlocked.Increment(ref closedCount); };
                connection.Connect(new IPEndPoint(IPAddress.Loopback, 8081));
                connections[i] = connection;
            }

            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(10);
                if (connectedCount == count)
                    break;
                if(closedCount > 0 )
                    throw new Exception("Connection closed!");
            }

            if (connectedCount < count)
                throw new Exception($"Failed to connect servers! (connected={connectedCount})");

            var clients = new ClientWorker[count];
            for (int i = 0; i < count; i++)
            {
                var server = new ServerRef(
                    new SlimActorRef { Id = 1 }, 
                    new SlimRequestWaiter(connections[i]),
                    null);
                clients[i] = new ClientWorker(server);
            }
            return clients;
        }
    }
}
