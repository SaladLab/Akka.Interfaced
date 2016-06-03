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
interface IGreetObserver : IInterfacedObserver
{
    void Event(string message);
}
```

And create new `IGreeterWithObserver` interface to make use of `IGreetObserver`.

```csharp
interface IGreeterWithObserver : IInterfacedActor
{
    Task<string> Greet(string name);
    Task<int> GetCount();

    // add an observer which receives a notification message whenever Greet request comes in
    Task Subscribe(IGreetObserver observer);

    // remove an observer
    Task Unsubscribe(IGreetObserver observer);
}
```

Define new `GreetingActor` using `IGreetObserver`.

```csharp
class GreetingActor : InterfacedActor, IGreeterWithObserver
{
    int _count;
    List<IGreetObserver> _observers = new List<IGreetObserver>();

    Task<string> IGreeterWithObserver.Greet(string name)
    {
        // send a notification 'Event' to all observers
        _observers.ForEach(o => o.Event($"Greet({name})"));
        _count += 1;
        return Task.FromResult($"Hello {name}!");
    }

    Task<int> IGreeterWithObserver.GetCount()
    {
        return Task.FromResult(_count);
    }

    Task IGreeterWithObserver.Subscribe(IGreetObserver observer)
    {
        _observers.Add(observer);
        return Task.FromResult(true);
    }

    Task IGreeterWithObserver.Unsubscribe(IGreetObserver observer)
    {
        _observers.Remove(observer);
        return Task.FromResult(true);
    }
}
```

Ok. Everything is ready.

#### Using an observer

One thing is left to play with `GreetingActor`.
Observer that receives notification messages.
For receiving events, we need to pass an observer to `GreetingActor` via `Subscribe` method.

```csharp
var greeter = new GreeterWithObserverRef(actor);
await greeter.Subscribe(/* CreateObserver<IGreetObserver>() */);
await greeter.Greet("World");
await greeter.Greet("Actor");
```

There are a few ways to create observer object.

##### Object Observer

When an actor sends notification, this observer will executes the event method in subject actor context.
This is not what we usually want to have.

First we need to write a class `GreetObserverDisplay` that inherits `IGreetObserver`.

```csharp
class GreetObserverDisplay : IGreetObserver
{
    void IGreetObserver.Event(string message)
    {
        // this will be executed in a subject actor execution context.
        Console.WriteLine($"Event: {message}");
    }
}
```

And create observer embedding `GreetObserverDisplay` and pass it to a subject actor.

```csharp
var greeter = new GreeterWithObserverRef(actor);
await greeter.Subscribe(ObjectNotificationChannel.Create<IGreetObserver>(new GreetObserverDisplay()));
await greeter.Greet("World");  // Output: Event: Greet(World)
await greeter.Greet("Actor");  // Output: Event: Greet(Actor)
```

`GreetObserverDisplay.Event` will be called directly by `GreetingActor`.
When a subject actor is located at remote node, an observer would be executed at that node.

##### Actor Observer

This Observer makes an observing actor handle notification messages.
Make an actor inherits `IGreetObserver` and create an object with `CreateObserver` method.

```csharp
class TestActor : InterfacedActor, IGreetObserver
{
    [MessageHandler]
    async Task Handle(string message)
    {
        var actor = Context.ActorOf<GreetingActor>();
        var greeter = new GreeterWithObserverRef(actor);
        await greeter.Subscribe(CreateObserver<IGreetObserver>());
        await greeter.Greet("World");
        await greeter.Greet("Actor");
    }

    void IGreetObserver.Event(string message)
    {
        // this will be executed in an observing actor execution context.
        Console.WriteLine($"Event: {message}");
    }
}

var actor = system.ActorOf<TestActor>();
actor.Tell("Test");
```

This observer will be executed in observing actor context and
this is the exact type of observer that you usually want to have.

##### SlimClient Object

This is similar with an object observer but observer event will be executed
in SlimClient which an observing object is located at.

```csharp
class GreetObserverDisplay : IGreetObserver
{
    void IGreetObserver.Event(string message)
    {
        Console.WriteLine($"Event: {message}");
    }
}
```

```csharp
var greeter = new GreeterWithObserverRef(actor);
await greeter.Subscribe(communicator.CreateObserver<IGreetObserver>(new GreetObserverDisplay()));
await greeter.Greet("World");  // Output: Event: Greet(World)
await greeter.Greet("Actor");  // Output: Event: Greet(Actor)
```
