## OnStart event

Actor has `PreStart` method for initializing itself on start.
But this method should be synchronous so it's not good for an asynchronous one.
To handle this, `OnStart` method is provided.

#### OnStart when an actor starts

`OnStart` event is fired when an actor starts or restarts right after `PreStart` or `PostRestart`,
and further messages are pending until `OnStart` finishes.

Following `MyActor` has `PreStart` and `OnStart` event handler.

```csharp
class MyActor : InterfacedActor
{
    override void PreStart()
    {
        Console.WriteLine("PreStart()");
    }

    override async Task OnStart(bool restarted)
    {
        Console.WriteLine($"OnStart() Begin");
        await Task.Delay(10);
        Console.WriteLine($"OnStart() End");
    }

    [MessageHandler]
    async Task Handle(string message)
    {
        Console.WriteLine($"Handle({message}) Begin");
        await Task.Delay(10);
        Console.WriteLine($"Handle({message}) End");
    }
}
```

You can see event order.

```csharp
var actor = System.ActorOf<MyActor>();
actor.Tell("Test");
// Output:
// PreStart()
// OnStart() Begin
// OnStart() End
// Handle(Test) Begin
// Handle(Test) End
```

#### OnStart when an actor restarts

`OnStart` is also fired when an actor restarts.
`restarted` tells whether it is starting or restarting.

```csharp
class MyActor2 : InterfacedActor
{
    override void PreStart()
    {
        Console.WriteLine("PreStart()");
    }

    override void PostRestart(Exception cause)
    {
        Console.WriteLine($"PostRestart({cause.GetType().Name})");
    }

    override async Task OnStart(bool restarted)
    {
        Console.WriteLine($"OnStart({restarted}) Begin");
        await Task.Delay(10);
        Console.WriteLine($"OnStart({restarted}) End");
    }

    [MessageHandler]
    async Task Handle(string message)
    {
        Console.WriteLine($"Handle({message}) Throw");
        throw new InvalidOperationException(message);
    }
}
```

`Handle(Test)` throws an unhandled exception and causes an actor to restart.

```csharp
var actor = _system.ActorOf<MyActor2>();
actor.Tell("Test");
// Output:
// PreStart()
// OnStart(False) Begin
// OnStart(False) End
// Handle(Test) Throw
// PostRestart(AggregateException)
// OnStart(True) Begin
// OnStart(True) End
```
