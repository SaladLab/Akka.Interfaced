## 0.5.4 (Released 2016/09/23)

* Update Akka 1.1.2

## 0.5.3 (Released 2016/07/16)

* Update Akka 1.1.1
* Add AkkaReceiverNotificationChannel.OverrideReceiver

## 0.5.2 (Released 2016/07/11)

* Add IRequestTarget.Cast instead of BoundActorTarget.Cast.
* Add InterfacedActorRef.Create.
* Add InterfacedMessageBuilder.

## 0.5.1 (Released 2016/07/07)

* Update Akka 1.1
* Change target type to ICanTell from IActorRef.

## 0.5.0 (Released 2016/07/04)

* Support generic interface and method. #30

## 0.4.1 (Released 2016/06/23)

* Add Akka.Interfaced.SlimServer module.
* Add InterfaceType property to InterfacedActorRef.
* Add InterfacedActorOf extension method to InterfacedActorRef.
* Add Cast extension method to InterfacedActorRef.

## 0.4.0 (Released 2016/06/20)

* Allow normal and slim client work together #29
* Sync actor handler & async observer handler. #26
* Support actor, observer interface inheritance. #27
* Refactoring ActorBoundSession. #24 #27
* Add observer context. #25
* Allow normal and slim client work together. #29
* Remove IDisposable from InterfacedObserver.
* Remove ctor(IActorRef) of generated observer class.
* Add ObjectNotificationChannel.
* Add warning log for receiving bad messages.
* Check a type of observer instance passed via rpc.
* Add missing fault handling of AkkaAskRequestWaiter.
* Add RequestHandlerNotFoundException.

## 0.3.2 (Released 2016/05/30)

* Add filter-order option to ResponsiveExceptionFilter.
* Fix another bug that filters can be used in wrong situation.

## 0.3.1 (Released 2016/05/29)

* Fix bug that filters can be used in wrong situation.
* Use RequestMessageException instead of InvalidMessageException.

## 0.3.0 (Released 2016/05/27)

* Enhance the readability of code setting observer. #15
* Let observer handler work with ExtendedHandler and Filter like Interfaced handler. #16
* Better error exception for invalid signature of extended handler. #17
* Utility for checking signature of extended handler. #18
* Rename a target argument of Whatever_Invoke.InvokeAsync #19
* Change InterfacedActor<T> to InterfacedActor. #20
* Exception policy for handling request, notification and message. #21
* Concise way for retrieving InterfacedActorRef on slim-client. #23

## 0.2.2 (Released 2016/05/03)

* Update Akka.NET 1.0.8

## 0.2.1 (Released 2016/04/25)

* Update Akka.NET 1.0.7

## 0.2.0 (Released 2015/11/09)

## 0.1.0 (Released 2015/11/08)

* Initial Release
