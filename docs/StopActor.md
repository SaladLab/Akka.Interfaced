** WRITING IN PROCESS **

## Stop an actor and OnGracefulStop event

- To stop an interfaced actor gracefully, send `InterfacedPoisonPill`.
- When an actor receives `InterfacedPoisonPill`, stop receiving further  messages and waits until all handler finish.
- If an actor stops not by `InterfacedPoisonPill`, OnGracefulStop is not called.

```csharp
public class TestActor : InterfacedActor, IWorker
{
    protected override async Task OnGracefulStop()
    {
        Console.WriteLine("OnGracefulStop() Begin");
        await Task.Delay(10);
        Console.WriteLine("OnGracefulStop() End");
    }

    protected override void PostStop()
    {
        Console.WriteLine("PostStop()");
    }
}
```

```csharp
var actor = ActorOf<TestActor>();
actor.Tell(InterfacedPoisonPill.Instance);
// Output:
// OnGracefulStop() Begin
// OnGracefulStop() End
// PostStop()
```
