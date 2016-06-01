** WRITING IN PROCESS **

## Exception

- When exception thrown, actor follows [default exception process]( http://getakka.net/docs/Fault%20tolerance) on Akka.net
- ResponsiveException filter can propagate some exceptions to caller not to actor supervisor.

```csharp
public class GreetingActor : InterfacedActor, IGreeter
{
    Task<string> IGreeter.Greet(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException(nameof(name));
        ...
    }
}
```

```csharp
try
{
    await greeter.Greet(null); // cause GreetingActor restart
}
catch (Exception e)
{
    Console.WriteLine(e);
    // Output: Akka.Interfaced.RequestFaultException ...
}
```

### ResponsiveException

```csharp
public class GreetingActor : InterfacedActor, IGreeter
{
    [ResponsiveException(typeof(ArgumentException))]
    Task<string> IGreeter.Greet(string name) { ... }
}
```

```csharp
try
{
    await greeter.Greet(null); // don't cause GreetingActor restart
}
catch (Exception e)
{
    Console.WriteLine("Exception: " + e.Message);
    // Output: System.ArgumentException: name ...
}
```
