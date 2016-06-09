** WRITING IN PROCESS **

## SlimClient

SlimClient is a client that can communicate with interfaced actors without Akka.NET dependency.
This was originally developed to let a client on .NET 3.5 work with a server on Akka.NET.

TODO: FIGURE (SlimClient, SlimServer, and Server on Akka.NET)

SlimClient
- Support .NET 3.5 and over (includig Unity3D)
- SlimClient doesn't depend on Akka.NET.

`Akka.Interfaced-SlimClient` itself doesn't provide any communication features
but just defines the common interface for all slim-client providers.

#### Features

SlimClient only communicate with interfaced actors.
  - Sends requests and receives responses.
  - Receives notifications with observers
  - Cannot send raw messages.

Cannot use any other features of Akka.NET such as creating actors.

#### [SlimHttp](https://github.com/SaladLab/Akka.Interfaced/tree/master/samples/SlimHttp)

This is a sample implementation for showing how they work.
HTTP is used to communicate.

At server, test actor `GreetingActor` is created and named as `greeter`.
Port 9000 is open for receiving requests from slim-clients.

```csharp
var greeter = System.ActorOf<GreetingActor>("greeter");
var httpConfig = new HttpSelfHostConfiguration("http://localhost:9000");
httpConfig.MapHttpAttributeRoutes();
var httpServer = new HttpSelfHostServer(httpConfig);
await httpServer.OpenAsync();
```

At client, a reference to server-side `greeter` actor is created and we can
send requests via this reference and get responses.

```csharp
var requestWaiter = new SlimRequestWaiter { Root = new Uri("http://localhost:9000") };
var greeter = new GreeterRef(new SlimActorRef("greeter"), requestWaiter);
Console.WriteLine(await greeter.Greet("World"));  // Output: Hello World!
Console.WriteLine(await greeter.Greet("Actor"));  // Output: Hello Actor!
Console.WriteLine(await greeter.GetCount());      // Output: 2
```

#### [SlimSocket](https://github.com/SaladLab/Akka.Interfaced.SlimSocket)

SlimSocket uses TCP for communication and protobuf-net for serialization.
Also it can communicate only with allowed actors with bound types, which is
good for keeping server secure.  

At server, `ClientGateway` actor is created to listen tcp connection.
When new connection is accepted, `ClientSession` actor is created and
an initial actor `EntryActor` is bound to slim-client to give an access to
reach other actors.

```csharp
void StartListen(ActorSystem system, int port) {
    var clientGateway = system.ActorOf(Props.Create(() => new ClientGateway(logger, CreateSession)));
    clientGateway.Tell(new ClientGatewayMessage.Start(new IPEndPoint(IPAddress.Any, port)));
}

IActorRef CreateSession(IActorContext context, Socket socket) =>
    context.ActorOf(Props.Create(() => new ClientSession(
        logger, socket, s_tcpConnectionSettings, CreateInitialActor)));

Tuple<IActorRef, Type>[] CreateInitialActor(IActorContext context, Socket socket) =>
    new[] {
        Tuple.Create(context.ActorOf(Props.Create(() => new EntryActor(context.Self))),
                     typeof(IEntry))
    };
```

At client, `Communicator` object is created to connect to a server gateway.
After establishing new connection, it gets the reference to an initial actor, `EntryActor`.
`EntryActor` creates `Greeter` and give the reference to an `GreetingActor`
back to client when `GetGreeter` method is called. Now you can access to `GreetingActor`.

```csharp
var communicator = new Communicator(...);
communicator.Start();

var entry = communicator.CreateRef<EntryRef>(1);
var greeter = await entry.GetGreeter();
Console.WriteLine(await greeter.Greet("World"));  // Output: Hello World!
Console.WriteLine(await greeter.Greet("Actor"));  // Output: Hello Actor!
Console.WriteLine(await greeter.GetCount());      // Output: 2
```
