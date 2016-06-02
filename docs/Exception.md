## Exception

When there is an unhandled exception is thrown in processing a request,
an actor supervisor will handle this and follow
[akka exception process](http://getakka.net/docs/Fault%20tolerance).

Usually root actor makes the actor throwing an exception restart if you don't
set fault configuration.

For example, `GreetingActor` throws an `ArgumentException`
if finds arguments are not valid at `Greet`.

```csharp
public class GreetingActor : InterfacedActor, IGreeter
{
    Task<string> IGreeter.Greet(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException(nameof(name));
        ...
    }
    ...
}
```

Running following code makes `GreetingActor` throw an unhandled exception and
restart. And caller will get `RequestFaultException` exception.

```csharp
await greeter.Greet(null);  // cause GreetingActor restart
                            // RequestFaultException is thrown
```

#### Responsive Exception

But previous unhandled exception `ArgumentException` is not severe for an
actor itself and it's better to forward an exact exception to a requester
instead of restarting the actor.

`ResponsiveException` is a filter that forwards specified exceptions to a request
rather than an actor supervisor. You can attach `ResponsiveException` to a method
or a class.

```csharp
public class GreetingActor : InterfacedActor, IGreeter
{
    [ResponsiveException(typeof(ArgumentException))]
    Task<string> IGreeter.Greet(string name) { ... }
    ...
}
```

When there is an unhandled exception specified at `ResponsiveException`,
it will be propagated to caller and goes on.

```csharp
await greeter.Greet(null);  // don't cause GreetingActor restart
                            // ArgumentException is thrown
}
```
