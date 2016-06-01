** WRITING IN PROCESS **

## LogFilter

- Monitor all requests, notifications and messages.
- Log library agnostic.
  - it supports any kinds of log library which meets requirements
    - class should have a logger member variable looks like '\*logger\*' or '\*log\*'.
    - logger should have following methods.
      - bool Is{logLevel}Enabled
      - void {logLevel}(string/object message)
    - Nlog and log4j meets requirements.

```csharp
[Log]
public class GreetingActor : InterfacedActor, IGreeter
{
    private NLog.ILogger _logger; // will be used for writing logs

    public TestActor()
    {
        _logger = NLog.LogManager.GetLogger("TestActor");
    }

    Task<string> IGreeter.Greet(string name) { ... }
    Task<int> IGreeter.GetCount() { ... }  
}
```

```csharp
var hello = await greeter.Greet("Hello");
// Log: <- (#-1) Greet {"name": "Hello"}
// Log: -> (#-1) Greet "Hello World!"

var count = await greeter.GetCount();
// Log: |LOG| <- (#-1) GetCount {}
// Log: |LOG| -> (#-1) GetCount 1
```
