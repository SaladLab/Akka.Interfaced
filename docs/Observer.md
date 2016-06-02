** WRITING IN PROCESS **

## Observer

When two actors are communicating with each other,
one has a client role which sends a request and waits for a response and
the other has a server-role which receives a request and reply a response.

But sometimes a server actor wants to send a notification message to a client.
In this case, `Observer` can be used.

#### Greeter with Observer

`GreetingActor` was a passive server actor. Let's add a notification that will
be sent to client whenever it gets a `Greet` request.

First of all, observer interface `IGreetObserver` need to be defined.

```csharp
public interface IGreetObserver : IInterfacedObserver
{
    void Event(string message);
}
```

And modify `IGreeter` interface to make use of `IGreetObserver`.

```csharp
public interface IGreeter : IInterfacedActor
{
    // add an observer which receives a notification message whenever Greet request comes in
    Task Subscribe(IGreetObserver observer);
    // remove an observer
    Task Unsubscribe(IGreetObserver observer);

    Task<string> Greet(string name);
}
```

Define new `GreetingActor` using `IGreetObserver`.

```csharp
public class GreetingActor : InterfacedActor, IGreeter
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

Ok. Everything is ready.

#### Using an observer

```csharp
var greeter = new GreeterRef(actor);
await greeter.Subscribe(/* CreateObserver<IGreetObserver>() */);
await greeter.Greet("World");
```

##### Raw observer

`ObjectNotificationChannel`

##### Actor observer

`ActorNotificationChannel`

Test actor is necessary for testing `GreetingActor` with an observer
because observer have to receive a notification meessage.

```csharp
public class TestActor : InterfacedActor, IGreetObserver
{
    [MessageHandler]
    private void Handle(string message)
    {
        var greeter = new GreeterRef(Context.ActorOf<GreetingActor>());
        greeter.Subscribe(CreateObserver<IGreetObserver>());
        var hello = await greeter.Greet("World");
        // Output: Event: Greet(World)
    }

    void IGreetObserver.Event(string message)
    {
        Console.WriteLine($"Event: {message}");
    }
}
```

##### SlimClient observer

`ObserverEventDispatcher`
