** WRITING IN PROCESS **

## SlimClient

- Support .NET 3.5 and over
  - Includes Unity3D
- SlimClient doesn't depend on AkkaNET.
  - SlimServer relies on AkkaNET because it is edge of akka world.

- Types
  - [SlimHttp](https://github.com/SaladLab/Akka.Interfaced/tree/master/samples/SlimHttp)
    - Communicate through HTTP.
  - [SlimSocket](https://github.com/SaladLab/Akka.Interfaced.SlimSocket)
    - Communicate through .NET socket
    - SlimClient can communicate with only allowed actors.
    - SlimClient uses protobuf-net for serializing messages
