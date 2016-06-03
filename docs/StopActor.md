## Stop an actor and OnGracefulStop event

There are a few ways to stop an actor. If you are not familar this topic, read
[How to Stop an Actor... the Right Way](https://petabridge.com/blog/how-to-stop-an-actor-akkadotnet/) first.

Stopping an interfaced actor gracefully, you need to send `InterfacedPoisonPill`
message to an actor instead of `PoisonPill`. When an actor receives
`InterfacedPoisonPill`,

- Stops receiving further messages.
- Waits for running reentrant handlers completed.
- Executes `OnGracefulStop` method.

```csharp
class MyActor : InterfacedActor
{
    [MessageHandler, Reentrant]
    async Task Handle(string message)
    {
        Console.WriteLine($"Handle({message}) Begin");
        await Task.Delay(10);
        Console.WriteLine($"Handle({message}) End");
    }

    override async Task OnGracefulStop()
    {
        Console.WriteLine("OnGracefulStop() Begin");
        await Task.Delay(10);
        Console.WriteLine("OnGracefulStop() End");
    }

    override void PostStop()
    {
        Console.WriteLine("PostStop()");
    }
}
```

You can see event order.

```csharp
var actor = ActorOf<TestActor>();
actor.Tell("Test");
actor.Tell(InterfacedPoisonPill.Instance);
// Output:
// Handle(Test) Begin
// Handle(Test) End
// OnGracefulStop() Begin
// OnGracefulStop() End
// PostStop()
```

But if you send `PoisonPill` to an interfaced actor, it halts reentrant handlers
and doesn't execute `OnGracefulStop` because it cannot recognize these things.
 
```csharp
var actor = ActorOf<TestActor>();
actor.Tell("Test");
actor.Tell(PoisonPill.Instance);
// Output:
// Handle(Test) Begin
// PostStop()
```
