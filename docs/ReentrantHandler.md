** WRITING IN PROCESS **

## Reentrant handler

```csharp
public class TestActor : InterfacedActor, IWorker
{
    async Task IWorker.Atomic(string name)
    {
        Console.WriteLine("Atomic({0}) Enter", name);
        await Task.Delay(10);
        Console.WriteLine("Atomic({0}) Mid", name);
        await Task.Delay(10);
        Console.WriteLine("Atomic({0}) Leave", name);
    }

    [Reentrant]
    async Task IWorker.Reentrant(string name)
    {
        Console.WriteLine("Reentrant({0}) Enter", name);
        await Task.Delay(10);
        Console.WriteLine("Reentrant({0}) Mid", name);
        await Task.Delay(10);
        Console.WriteLine("Reentrant({0}) Leave", name);
    }
}
```

```csharp
var w = new WorkerRef(actor);

await Task.WhenAll(
    w.Atomic("A"),
    w.Atomic("B"));

// Output:
// Atomic(A) Enter
// Atomic(A) Mid
// Atomic(A) Leave
// Atomic(B) Enter
// Atomic(B) Mid
// Atomic(B) Leave

await Task.WhenAll(
    w.Reentrant("A"),
    w.Reentrant("B"));

// Output:
// Reentrant(A) Enter
// Reentrant(B) Enter
// Reentrant(A) Mid
// Reentrant(B) Mid
// Reentrant(A) Leave
// Reentrant(B) Leave
```

- Running reentrant handler can be halted because actor stops.
  - Caller will receive `RequestHaltException`

- Reentrant handler can run for long time.
  - With stop flag, it can be regarded as a single-threaded work function.  
