## Project configuration

#### Projects

There should be at least two projects because an interfaced actor uses post-build
code generator to write payload classes, ref classes, etc.

Two projects are

- One is an interface project including interfaces for actor and observer.
- The other is a main project including everything except interfaces.

For example, `HelloWorld` example solution consists of two projects:

- Project `HelloWorld.Interface`
  - IGreeter.cs
  - Depends on `Akka.Interfaced.Templates` nuget-package
- Project `HelloWorld.Program`
  - GreetingActor.cs
  - Program.cs
  - Depends on `HelloWorld.Interface`
  - Depends on `Akka.Interfaced` nuget-package

#### Install DeadRequestProcessingActor

You can send a request to an actor with an interfaced reference
and waits for a reponse.

```csharp
GreeterRef greeter = ...;
var hello = await greeter.Greet("Hello"); // can wait indefinitely
```

When akka.net forwards a message whose recipient is not found, which makes
a sender wait a long time or even indefinitely.
To avoid this problem, `DeadRequestProcessingActor` should be installed
right after creating an actor system.

```csharp
var system = ActorSystem.Create("MySystem");
DeadRequestProcessingActor.Install(system);
```

When `DeadRequestProcessingActor` is installed and there is no actor for
a request, exception is thrown immediately.

```csharp
GreeterRef greeter = ...;
var hello = await greeter.Greet("Hello"); // when no greeter, RequestTargetException is thrown.
```

#### Configure actor serializer

When a project uses multiple instances of actor system, Akka.Remote could be used.
Akka.Remote uses customizable serializers to send and receive a message through a network.

Default akka serializer (json.net or wire) is good to use, but when you want to
squeeze every drop of network bandwidth, ProtobufSerializer can serve.

To use protobuf-serializer, additional packages need to be installed.

- For an interface project,
  `Akka.Interfaced.Templates-Protobuf` need to be installed *instead of* `Akka.Interfaced.Templates`
- For a main project,
  `Akka.Interfaced.ProtobufSerializer` need to be installed *in addition to* `Akka.Interfaced`

Also the actor system is configured as following.

```csharp
var commonConfig = ConfigurationFactory.ParseString(@"
    akka {
      actor {
        provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
        serializers {
          wire = ""Akka.Serialization.WireSerializer, Akka.Serialization.Wire""
          proto = ""Akka.Interfaced.ProtobufSerializer.ProtobufSerializer, Akka.Interfaced.ProtobufSerializer""
        }
        serialization-bindings {
          ""Akka.Interfaced.NotificationMessage, Akka.Interfaced-Base"" = proto
          ""Akka.Interfaced.RequestMessage, Akka.Interfaced-Base"" = proto
          ""Akka.Interfaced.ResponseMessage, Akka.Interfaced-Base"" = proto
          ""System.Object"" = wire
        }
      }
      remote {
        helios.tcp {
          hostname = localhost
        }
      }
    }");
var config = commonConfig.WithFallback("akka.remote.helios.tcp.port = 9001");
var system = ActorSystem.Create("Server", config);
```

#### Slim client

Detailed explanation is not here :)

- For an interface project,
  `Akka.Interfaced-SlimClient.Templates` or `Akka.Interfaced-SlimClient.Templates-Protobuf`
  need to be installed *instead of* `Akka.Interfaced.Templates`
