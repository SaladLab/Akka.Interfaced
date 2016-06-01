** WRITING IN PROCESS **

## Defining an interface

```csharp
public interface IGreeter : IInterfacedActor
{
    Task<string> Greet(string name);
    Task<int> GetCount();
}
```

Return type of all method of interface should be Task.
