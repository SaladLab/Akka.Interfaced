## Reentrant handler

An actor handles all messages in a serialized fashion. It is an important
aspect to keep programming with akka.net easy and safe.
It's same with an asynchronous handler.

For example, `Greet` method of `GreetingActor` is asynchronous method that
takes a time to finish work.

```csharp
public class GreetingActor : InterfacedActor, IGreeter
{
    async Task<string> IGreeter.Greet(string name)
    {
        _count += 1;
        Console.WriteLine($"Greet({name}) Begin");
        await Task.Delay(10);
        Console.WriteLine($"Greet({name}) End");
        return $"Hello {name}!";
    }
    Task<int> IGreeter.GetCount()
    {
        Console.WriteLine("GetCount()");
        return Task.FromResult(_count);
    }
}
```

When you send multiple requests at one go, actor arranges requests in a row and
processes a request at a time.

```csharp
var greeter = new GreeterRef(actor);
await Task.WhenAll(
    greeter.Greet("A"),
    greeter.Greet("B"),
    greeter.GetCount());
// Output:
// Greet(A) Begin
// Greet(A) End
// Greet(B) Begin
// Greet(B) End
// GetCount()    
```

Fine. But how about making `Greet` method reentrant which means that an actor
can process other requests while handling a `Greet` request?
It makes the actor more responsive and
you can make a method reentrant by attaching `[Reentrant]` attribute.

```csharp
public class GreetingActor : InterfacedActor, IGreeter
{
    [Reentrant] async Task<string> IGreeter.Greet(string name) { ... }
}
```

Because `Greet` handler is reentrant, next requests can be handled in awaiting
`Task.Delay` in `Greet`.

```csharp
var greeter = new GreeterRef(actor);
await Task.WhenAll(
    greeter.Greet("A"),
    greeter.Greet("B"),
    greeter.GetCount());
// Output:
// Greet(A) Begin
// Greet(B) Begin
// GetCount()    
// Greet(A) End
// Greet(B) End
```

Even for a reentrant handler, code before and after `await` is serialized and
you can assume safely that it keeps running mutual exclusively.
Because of this, you don't need to use lock or interlocked operation for
chainging `_count` value at `Greet`.

```csharp
[Reentrant]
async Task<string> IGreeter.Greet(string name)
{
    _count += 1;  // safe without any synchronization mechanism.
    await ...
}
```

When an actor stops with ongoing reentrant requests, actor sends an exception,
`RequestHaltException` to a requester.

#### Writing a safe reentrant handler

Reentrant handler is good to make an actor responsive at a cost of increased
complexity. Be careful of writing mutable code in reentrant handlers.

For example, following `GreetingActor` has a problem for mutating its state.

```csharp
public class GreetingActor : InterfacedActor, IGreeter
{
    private List<string> _history = new List<string>();

    [Reentrant] async Task<string> IGreeter.Greet(string name)
    {
        _history.Add("*");
        await Task.Delay(10);
        _history[_history.Count - 1] = name;  // _history.Count may be changed after Add
        return $"Hello {name}!";
    }
}
```

Following code fixes this problem.

```csharp
public class GreetingActor : InterfacedActor, IGreeter
{
    private List<string> _history = new List<string>();

    [Reentrant] async Task<string> IGreeter.Greet(string name)
    {
        _history.Add("*");
        var index = _history.Count - 1;       // keep my index
        await Task.Delay(10);
        _history[index] = name;               // _history may be modified but it's ok
        return $"Hello {name}!";
    }
}
```

#### Background work handler

Reentrant handler can be used as a background worker like following.
Service handler keeps working until a stop signal.

```csharp
public class ServingActor : InterfacedActor, IServer
{
    private bool _stopped;

    [Reentrant] async Task Service()
    {
        while (_stopped == false)
        {
            await Task.Delay(1000);
            await DoServiceAsync();
        }
    }

    ...
}
```
