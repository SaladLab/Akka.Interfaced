** WRITING IN PROCESS **

## Message handler

In addition to interfaced request, interfaced actor can handle a regular message in a declarative way.
If you attach `[MessageHandler]` attribute to method and incoming message
whose type is same with type of first argument of handler, handler processes it.

```csharp
public class GreetingActor : InterfacedActor, IGreeter
{
    [MessageHandler] // handle string type message
    private void Handle(string message)
    {
        Console.WriteLine($"HandleString({message})");
    }

    [MessageHandler] // handle int type message
    private void Handle(int message)
    {
        Console.WriteLine($"HandleInt({message})");
    }
}
```

```csharp
IActorRef actor = GetOrCreateActorRef<GreetingActor>();
actor.Tell("Hello");  // Output: HandleString(Hello)
actor.Tell(1);        // Output: HandleInt(1)
```
