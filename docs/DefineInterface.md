## Defining an interface

Define an interface for actor like an ordinary inteface except two things.
- Inherits `IInterfacedActor`.
- The return type of all methods should be Task or Task\<T\>.

For example, following source defines `IGreeter` which has two methods.

```csharp
public interface IGreeter : IInterfacedActor
{
    // Greet: (string) -> (string)
    Task<string> Greet(string name);
    // GetCount: () -> (int)
    Task<int> GetCount();
}
```
