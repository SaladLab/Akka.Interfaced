** WRITING IN PROCESS **

## OnStart event

- OnStart event is fired when actor starts or restarts right after PreStart or PostRestart
- Following messages starts to be handled after finishing running OnStart.

```csharp
public class TestActor : InterfacedActor, IWorker
{
    protected override void PreStart()
    {
        Console.WriteLine("PreStart()");
    }

    protected override async Task OnStart(bool restarted)
    {
        Console.WriteLine("OnStart() Begin");
        await Task.Delay(10);
        Console.WriteLine("OnStart() End");
    }
}
```

```csharp
var actor = System.ActorOf<TestActor>();
// Output:
// PreStart()
// OnStart() Begin
// OnStart() End
```
