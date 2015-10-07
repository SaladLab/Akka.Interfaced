using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Interfaced;
using PingpongInterface;

namespace PingpongProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            DoLocalTest(1000, 1000);
            // DoRemoteTest(100, 1000);
        }

        private static void DoLocalTest(int actorCount, int pingCount)
        {
            using (var system = ActorSystem.Create("System"))
            {
                DeadRequestProcessingActor.Install(system);

                // make clients and servers

                var t1 = new Stopwatch();
                t1.Start();

                var clients = new List<ClientRef>();
                for (int i = 0; i < actorCount; i++)
                {
                    var server = system.ActorOf(Props.Create<ServerActor>(), "Server_" + i);
                    var client = system.ActorOf(Props.Create<ClientActor>(server), "Client_" + i);
                    clients.Add(new ClientRef(client));
                }

                var warmUpTasks = clients.Select(x => x.Start(1)).ToArray();
                Task.WaitAll(warmUpTasks);

                t1.Stop();
                Console.WriteLine($"Ready: {t1.Elapsed}");

                // test

                var t2 = new Stopwatch();
                t2.Start();

                var pingTasks = clients.Select(x => x.Start(pingCount)).ToArray();
                Task.WaitAll(pingTasks);

                t2.Stop();
                Console.WriteLine($"Ready: {t2.Elapsed}");
            }
        }

        private static void DoRemoteTest(int actorCount, int pingCount)
        {
            // make clients and servers

            var t1 = new Stopwatch();
            t1.Start();

            var servers = CreateRemoteServers(actorCount);
            var clients = CreateRemoteClients(actorCount);

            var warmUpTasks = clients.Select(x => x.Start(1)).ToArray();
            Task.WaitAll(warmUpTasks);

            t1.Stop();
            Console.WriteLine($"Ready: {t1.Elapsed}");

            // test

            var t2 = new Stopwatch();
            t2.Start();

            var pingTasks = clients.Select(x => x.Start(pingCount)).ToArray();
            Task.WaitAll(pingTasks);

            t2.Stop();
            Console.WriteLine($"Ready: {t2.Elapsed}");
        }

        private static IActorRef[] CreateRemoteServers(int count)
        {
            var config = ConfigurationFactory.ParseString(@"
                akka {  
                    actor {
                        provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                        serializers {
                          proto = ""Akka.Interfaced.ProtobufSerializer.ProtobufSerializer, Akka.Interfaced-ProtobufSerializer""
                        }
                        serialization-bindings {
                          ""Akka.Interfaced.NotificationMessage, Akka.Interfaced"" = proto
                          ""Akka.Interfaced.RequestMessage, Akka.Interfaced"" = proto
                          ""Akka.Interfaced.ResponseMessage, Akka.Interfaced"" = proto
                        }
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
            DeadRequestProcessingActor.Install(system);

            var servers = new IActorRef[count];
            for (int i = 0; i < count; i++)
            {
                var server = system.ActorOf(Props.Create<ServerActor>(), "Server_" + i);
                servers[i] = server;
            }
            return servers;
        }

        private static ClientRef[] CreateRemoteClients(int count)
        {
            var config = ConfigurationFactory.ParseString(@"
                akka {  
                    actor {
                        provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                        serializers {
                          proto = ""Akka.Interfaced.ProtobufSerializer.ProtobufSerializer, Akka.Interfaced-ProtobufSerializer""
                        }
                        serialization-bindings {
                          ""Akka.Interfaced.NotificationMessage, Akka.Interfaced"" = proto
                          ""Akka.Interfaced.RequestMessage, Akka.Interfaced"" = proto
                          ""Akka.Interfaced.ResponseMessage, Akka.Interfaced"" = proto
                        }
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
            DeadRequestProcessingActor.Install(system);

            var clients = new ClientRef[count];
            for (int i = 0; i < count; i++)
            {
                var serverPath = $"akka.tcp://ServerSystem@127.0.0.1:8081/user/Server_{i}";
                var server = system.ActorSelection(serverPath).ResolveOne(TimeSpan.Zero).Result;
                var client = system.ActorOf(Props.Create<ClientActor>(server), "Client_" + i);
                clients[i] = new ClientRef(client);
            }
            return clients;
        }
    }
}
