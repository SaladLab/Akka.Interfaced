## Message handler

An actor can handle a messages. Actor that inherits `[ReceiveActor]` can define which handler processes which message.

For example, following `MyActor` can handle a message whose type is
`string` or `int`.  

```csharp
public class MyActor : ReceiveActor
{
    public MyActor()
    {
        Receive<string>(message =>
        {
            Console.WriteLine($"HandleString({message})");
        });
        Receive<int>(message =>
        {
            Console.WriteLine($"HandleInt({message})");
        });
    }
}

actor.Tell("Hello");  // Output: HandleString(Hello)
actor.Tell(1);        // Output: HandleInt(1)
```

In addition to a interfaced request, an interfaced actor can handle
this regular message in a declarative way.
If you attach `[MessageHandler]` attribute to a method and an incoming message
whose type is same with the type of first argument of handler,
this method handles it.

```csharp
public class MyActor : InterfacedActor, ...
{
    [MessageHandler]
    private void Handle(string message)
    {
        Console.WriteLine($"HandleString({message})");
    }

    [MessageHandler]
    private void Handle(int message)
    {
        Console.WriteLine($"HandleInt({message})");
    }
}

actor.Tell("Hello");  // Output: HandleString(Hello)
actor.Tell(1);        // Output: HandleInt(1)
```

Message handler can be asynchronous and have `[Reentrant]` attribute.
