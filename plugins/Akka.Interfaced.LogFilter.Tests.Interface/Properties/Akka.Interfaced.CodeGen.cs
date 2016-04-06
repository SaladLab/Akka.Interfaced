﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Akka.Interfaced CodeGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;

#region Akka.Interfaced.LogFilter.Tests.ITest

namespace Akka.Interfaced.LogFilter.Tests
{
    [PayloadTableForInterfacedActor(typeof(ITest))]
    public static class ITest_PayloadTable
    {
        public static Type[,] GetPayloadTypes()
        {
            return new Type[,] {
                { typeof(Call_Invoke), null },
                { typeof(CallWithActor_Invoke), null },
                { typeof(GetHelloCount_Invoke), typeof(GetHelloCount_Return) },
                { typeof(SayHello_Invoke), typeof(SayHello_Return) },
            };
        }

        public class Call_Invoke
            : IInterfacedPayload, IAsyncInvokable
        {
            public System.String value;
            public Type GetInterfaceType() { return typeof(ITest); }
            public async Task<IValueGetable> InvokeAsync(object target)
            {
                await ((ITest)target).Call(value);
                return null;
            }
        }

        public class CallWithActor_Invoke
            : IInterfacedPayload, IAsyncInvokable
        {
            public Akka.Interfaced.LogFilter.Tests.TestRef test;
            public Type GetInterfaceType() { return typeof(ITest); }
            public async Task<IValueGetable> InvokeAsync(object target)
            {
                await ((ITest)target).CallWithActor(test);
                return null;
            }
        }

        public class GetHelloCount_Invoke
            : IInterfacedPayload, IAsyncInvokable
        {
            public Type GetInterfaceType() { return typeof(ITest); }
            public async Task<IValueGetable> InvokeAsync(object target)
            {
                var __v = await ((ITest)target).GetHelloCount();
                return (IValueGetable)(new GetHelloCount_Return { v = __v });
            }
        }

        public class GetHelloCount_Return
            : IInterfacedPayload, IValueGetable
        {
            public System.Int32 v;
            public Type GetInterfaceType() { return typeof(ITest); }
            public object Value { get { return v; } }
        }

        public class SayHello_Invoke
            : IInterfacedPayload, IAsyncInvokable
        {
            public System.String name;
            public Type GetInterfaceType() { return typeof(ITest); }
            public async Task<IValueGetable> InvokeAsync(object target)
            {
                var __v = await ((ITest)target).SayHello(name);
                return (IValueGetable)(new SayHello_Return { v = __v });
            }
        }

        public class SayHello_Return
            : IInterfacedPayload, IValueGetable
        {
            public System.String v;
            public Type GetInterfaceType() { return typeof(ITest); }
            public object Value { get { return v; } }
        }
    }

    public interface ITest_NoReply
    {
        void Call(System.String value);
        void CallWithActor(Akka.Interfaced.LogFilter.Tests.ITest test);
        void GetHelloCount();
        void SayHello(System.String name);
    }

    public class TestRef : InterfacedActorRef, ITest, ITest_NoReply
    {
        public TestRef(IActorRef actor) : base(actor)
        {
        }

        public TestRef(IActorRef actor, IRequestWaiter requestWaiter, TimeSpan? timeout) : base(actor, requestWaiter, timeout)
        {
        }

        public ITest_NoReply WithNoReply()
        {
            return this;
        }

        public TestRef WithRequestWaiter(IRequestWaiter requestWaiter)
        {
            return new TestRef(Actor, requestWaiter, Timeout);
        }

        public TestRef WithTimeout(TimeSpan? timeout)
        {
            return new TestRef(Actor, RequestWaiter, timeout);
        }

        public Task Call(System.String value)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new ITest_PayloadTable.Call_Invoke { value = value }
            };
            return SendRequestAndWait(requestMessage);
        }

        public Task CallWithActor(Akka.Interfaced.LogFilter.Tests.ITest test)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new ITest_PayloadTable.CallWithActor_Invoke { test = (Akka.Interfaced.LogFilter.Tests.TestRef)test }
            };
            return SendRequestAndWait(requestMessage);
        }

        public Task<System.Int32> GetHelloCount()
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new ITest_PayloadTable.GetHelloCount_Invoke {  }
            };
            return SendRequestAndReceive<System.Int32>(requestMessage);
        }

        public Task<System.String> SayHello(System.String name)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new ITest_PayloadTable.SayHello_Invoke { name = name }
            };
            return SendRequestAndReceive<System.String>(requestMessage);
        }

        void ITest_NoReply.Call(System.String value)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new ITest_PayloadTable.Call_Invoke { value = value }
            };
            SendRequest(requestMessage);
        }

        void ITest_NoReply.CallWithActor(Akka.Interfaced.LogFilter.Tests.ITest test)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new ITest_PayloadTable.CallWithActor_Invoke { test = (Akka.Interfaced.LogFilter.Tests.TestRef)test }
            };
            SendRequest(requestMessage);
        }

        void ITest_NoReply.GetHelloCount()
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new ITest_PayloadTable.GetHelloCount_Invoke {  }
            };
            SendRequest(requestMessage);
        }

        void ITest_NoReply.SayHello(System.String name)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new ITest_PayloadTable.SayHello_Invoke { name = name }
            };
            SendRequest(requestMessage);
        }
    }
}

#endregion
