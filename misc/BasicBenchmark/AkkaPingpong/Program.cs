using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;

namespace AkkaPingpong
{
    class Program
    {
        static void Main(string[] args)
        {
            // DoLocalTest(1000, 1000);
            DoRemoteTest(1000, 1000);
        }

        private static void DoLocalTest(int actorCount, int pingCount)
        {
            using (var system = ActorSystem.Create("System"))
            {
                // make clients and servers

                var t1 = new Stopwatch();
                t1.Start();

                var clients = new List<IActorRef>();
                for (int i = 0; i < actorCount; i++)
                {
                    var server = system.ActorOf(Props.Create<ServerActor>(), "Server_" + i);
                    var client = system.ActorOf(Props.Create<ClientActor>(server), "Client_" + i);
                    clients.Add(client);
                }

                var warmUpTasks = clients.Select(x =>
                    x.Ask<ClientMessages.StartResponse>(
                        new ClientMessages.StartRequest { Count = 1 })
                ).ToArray();
                Task.WaitAll(warmUpTasks);

                t1.Stop();
                Console.WriteLine($"Ready: {t1.Elapsed}");

                // test

                var t2 = new Stopwatch();
                t2.Start();

                var pingTasks = clients.Select(x =>
                    x.Ask<ClientMessages.StartResponse>(
                        new ClientMessages.StartRequest { Count = pingCount })
                ).ToArray();
                Task.WaitAll(pingTasks);

                t2.Stop();
                Console.WriteLine($"Test: {t2.Elapsed}");
            }
        }

        private static void DoRemoteTest(int actorCount, int pingCount)
        {
            // make clients and servers

            var t1 = new Stopwatch();
            t1.Start();

            var servers = CreateRemoteServers(actorCount);
            var clients = CreateRemoteClients(actorCount);

            var warmUpTasks = clients.Select(x =>
                x.Ask<ClientMessages.StartResponse>(
                    new ClientMessages.StartRequest { Count = 1 })
            ).ToArray();
            Task.WaitAll(warmUpTasks);

            t1.Stop();
            Console.WriteLine($"Ready: {t1.Elapsed}");

            // test

            var t2 = new Stopwatch();
            t2.Start();

            var pingTasks = clients.Select(x =>
                x.Ask<ClientMessages.StartResponse>(
                    new ClientMessages.StartRequest { Count = pingCount })
            ).ToArray();
            Task.WaitAll(pingTasks);

            t2.Stop();
            Console.WriteLine($"Test: {t2.Elapsed}");
        }

        private static IActorRef[] CreateRemoteServers(int count)
        {
            var config = ConfigurationFactory.ParseString(@"
                akka {
                    actor {
                        provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                    }
                    remote {
                        helios.tcp {
                            transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
                            transport-protocol = tcp
                            port = 8081
                            hostname = 127.0.0.1
                        }
                    }
                }");

            var system = ActorSystem.Create("ServerSystem", config);

            var servers = new IActorRef[count];
            for (int i = 0; i < count; i++)
            {
                var server = system.ActorOf(Props.Create<ServerActor>(), "Server_" + i);
                servers[i] = server;
            }
            return servers;
        }

        private static IActorRef[] CreateRemoteClients(int count)
        {
            var config = ConfigurationFactory.ParseString(@"
                akka {
                    actor {
                        provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                    }
                    remote {
                        helios.tcp {
                            transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
                            transport-protocol = tcp
                            hostname = 127.0.0.1
                        }
                    }
                }");

            var system = ActorSystem.Create("ClientSystem", config);

            var clients = new IActorRef[count];
            for (int i = 0; i < count; i++)
            {
                var serverPath = $"akka.tcp://ServerSystem@127.0.0.1:8081/user/Server_{i}";
                var server = system.ActorSelection(serverPath).ResolveOne(TimeSpan.Zero).Result;
                var client = system.ActorOf(Props.Create<ClientActor>(server), "Client_" + i);
                clients[i] = client;
            }
            return clients;
        }
    }
}
