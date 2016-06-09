## PersistentActor

`UntypedPersistentActor` is the special base class the provides the persistency
to derived actors using eventsourcing.
`InterfacedPersistentActor` is a `UntypedPersistentActor` with features of `InterfacedActor`.

- `InterfacedActor` = the interfaced class of `UntypedActor`
- `InterfacedPersistentActor` = the interfaced class of `UntypedPersistentActor`

If you are not familar `Akka.Persistence`, read [Persistent Actors](http://getakka.net/docs/persistence/persistent-actors)

#### Example

At first, you need to define event classes and a (optional) state class.
The event journal will handle those classes to keep an actor persistent.

```csharp
class GreetEvent { public string Name; }

class GreeterState
{
    public int GreetCount;
    public int TotalNameLength;

    public void OnGreet(GreetEvent e)
    {
        GreetCount += 1;
        TotalNameLength += e.Name.Length;
    }
}
```

Define `PersistentGreetingActor` that inherits `InterfacedPersistentActor` and
implements `IGreeter`. This greeting class will be persistent with `OnRecover` method
which recover the actor state from event messages or (optional) snapshot message.

```csharp
class PersistentGreetingActor : InterfacedPersistentActor, IGreeter
{
    private GreeterState _state = new GreeterState();

    public override string PersistenceId { get; }

    public PersistentGreetingActor(string id)
    {
        PersistenceId = id;
    }

    [MessageHandler] void OnRecover(SnapshotOffer snapshot)
    {
        _state = (GreeterState)snapshot.Snapshot;
    }

    [MessageHandler] void OnRecover(GreetEvent e)
    {
        _state.OnGreet(e);
    }

    async Task<string> IGreeter.Greet(string name)
    {
        var e = new GreetEvent { Name = name };
        await PersistTaskAsync(e);
        _state.OnGreet(e);
        return $"Hello {name}!";
    }

    Task<int> IGreeter.GetCount()
    {
        return Task.FromResult(_state.GreetCount);
    }
}
```

You can use `PersistentGreetingActor` as a normal greeting actor but the state of
greeting actor is kept when it restarts.

```csharp
// create actor, change state of it, and destroy it.
var a = _system.ActorOf(Props.Create(() => new PersistentGreetingActor("greeter1")));
var g = new GreeterRef(a);
await g.Greet("World");
await g.Greet("Actor");
Console.WriteLine("1st: " + await g.GetCount());   // Output: 1st: 2
await a.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

// create actor, and check saved state.
var a2 = _system.ActorOf(Props.Create(() => new PersistentGreetingActor("greeter1")));
var g2 = new GreeterRef(a2);
Console.WriteLine("2nd: " + await g2.GetCount());  // Output: 2nd: 2
await g2.Greet("More");
Console.WriteLine("3rd: " + await g2.GetCount());  // Output: 3rd: 3
await a2.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);
```
