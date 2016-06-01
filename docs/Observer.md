** WRITING IN PROCESS **

## Observer

Define observer interface:

```csharp
public interface IGreetObserver : IInterfacedObserver
{
    void Event(string message);
}
```

Define new `IGreeter` using `IGreetObserver`.

```csharp
public interface IGreeter : IInterfacedActor
{
    Task Subscribe(IGreetObserver observer);
    Task Unsubscribe(IGreetObserver observer);
    Task<string> Greet(string name);
}
```

Define new `GreetingActor` using `IGreetObserver`.

```csharp
public interface IGreeter : InterfacedActor, IGreeter
{
    List<IGreetObserver> _observers = new List<IGreetObserver>();

    Task IGreeter.Subscribe(IGreetObserver observer)
    {
        _observers.Add(observer);
        return Task.FromResult(true);
    }

    Task IGreeter.Unsubscribe(IGreetObserver observer)
    {
        _observers.Remove(observer);
        return Task.FromResult(true);
    }

    Task<string> IGreeter.Greet(string name)
    {
        // send a notification 'Event' to all observers
        _observers.ForEach(o => o.Event($"Greet({name})"))
        return Task.FromResult($"Hello {name}!");
    }

}
```

Define a test actor which can receive notification with observer and
start test.

```csharp
public class TestActor : InterfacedActor, IGreetObserver
{
    [MessageHandler]
    private void Handle(string message)
    {
        var greeter = new GreeterRef(Context.ActorOf<GreetingActor>());
        greeter.Subscribe(CreateObserver<IGreetObserver>());
        var hello = await greeter.Greet("World"); // Output: Event: Greet(World)
    }

    void IGreetObserver.Event(string message)
    {
        Console.WriteLine($"Event: {message}");
    }
}
```

- Notification order
