*Work in progress*

## Serialization

Some issues about serialization.

##### Polymorphism

When you see a parameter typed `object`, you expect that this can be
everything derived from `object`.

```csharp
public interface IGreeter : IInterfacedActor
{
    Task<object> Greet(object name);
    ...
}
```

In only local messaging, it's totally fine.
But in remote messaging with `Akka.Remote`, it depends on your serializer.

TODO

##### Tolerance for the changes of signature

Interface changes and two nodes communicating with each other could be a product
with different version. In this situation, tolerance for the change of interface
signature becomes important.

C# does allow you to do following changes without making breaking changes.

TODO: Which side is changed is important!
Following covers server-side changes.

```csharp
// Add result value
   Task         Greet(string name);
-> Task<string> Greet(string name);
```

```csharp
// Add optional parameters
   Task<string> Greet(string name);
-> Task<string> Greet(string name, string nickname = "");
```

```csharp
// Change the type of parameter to a contravariant type.
   Task<string> Greet(string name);
-> Task<string> Greet(object name);
```

```csharp
// Change the type of return value to a covariant type.
   Task<string> Greet(string name);
-> Task<string> Greet(object name);
```

```csharp
// Change the name of method
   Task<string> Greet(string name);
-> Task<string> SayHello(string name);
```

```csharp
// Change the type of parameter
   Task<string> Greet(string name);
-> Task<string> Greet(string name);
```

```csharp
// Original
Task<string> SayHello(string name);
// Rename method
Task<string> SayHello(string name);

// Rename parameter
Task<string> Greet(string guestName);

// Add a parameter
Task<string> Greet(string name, string nick);

// Add a parameter
Task<string> Greet(string name, string nick = 10);

// Remove a parameter
Task<string> Greet();

// Change the type of return value
Task<Tuple<string, string>> Greet(string name);
```

##### Footprints

Foot print.

### Comparison

##### Wire

-
- No Tolerance

##### protobuf-net

- No polymorphism
- Tolerable
##### Json.NET
