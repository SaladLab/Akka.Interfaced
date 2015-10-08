## Purpose

 - To check max actor messaging throuput in network environment.

## Test Scenario

 - There are N Clients
 - There are N Servers
 - Each client sends echo request to its server.
 - When a server gets an echo request, it sends an echo response to a client.
 - When a client gets an echo response, it sends next echo request to a server.
 - Client repeats this M times
 - N clients do this simultaneously

## Test Programs

All test program will spawn clients and servers in one program and run them in same machine.
There are 3 distint test programs.

#### AkkaPingpong

 - Implemented with plain Akka.NET actor model.
 - All client and server class are derived class of ReceiveActor.
 - Actors don't use any async/await code to keep lightweight.

#### InterfacedPingpong

 - Implememnt with Akka.Interfaced actor model.
 - All client and server class are derived class of InterfacedActor.
 - Client actor uses async/await pattern for code readability.
 - Server actor doesn't use async/await pattern because not necessary.

#### SlimSocketPingpong

 - Implement with Akka.Interfaced actor model.
 - Server class is same with InterfacedPingpong one.
 - Client class uses SlimClient and it's not an actor.
 - SlimSocket network layer is used for transporting message between client and server.

## Test Environment

 - CPU: Intel Core i5-3570 3.40GHz
 - RAM: 16.0GB
 - OS: Windows 10 Pro
 - Build: .NET 4.6 / Release Build (.NET GC: server=enabled, concurrent=disabled)

## Test Result

 - AkkaPingpong
   - **10356** req/sec
 - InterfacedPingpong
   - **12642** req/sec (x1.2)
   - a little faster than AkkaPingpong. it's caused by fast serialization.
 - SlimSocketPingpong
   - **53022** req/sec (x5.1)
   - a way faster than the others. Because slim socket has little work for transport.

## Raw Data

#### AkkaPingpong

N=100 M=100
```
Ready: 00:00:00.6146966
Test:  00:00:01.2417426
```

N=1000 M=1000
```
Ready: 00:00:01.1517478
Test:  00:01:36.5672103
```

#### InterfacedPingpong

N=100 M=100
```
Ready: 00:00:00.8785921
Test:  00:00:01.1588795
```

N=1000 M=0100
```
Ready: 00:00:01.3156256
Test:  00:01:19.1018168
```

#### SlimSocketPingpong

N=100 M=100
```
Ready: 00:00:02.9131035
Test:  00:00:00.1918747
```

N=1000 M=1000
```
Ready: 00:00:25.6235302
Test:  00:00:18.8610649
```

## Akka version

 - Akka.NET 1.0.4
 - Akka.Interfaced 0.1.0-dev20151008
