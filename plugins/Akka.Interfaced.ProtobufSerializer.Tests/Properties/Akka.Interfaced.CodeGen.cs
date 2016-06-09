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
using ProtoBuf;
using TypeAlias;
using System.ComponentModel;

#region Akka.Interfaced.ProtobufSerializer.Tests.IDefault

namespace Akka.Interfaced.ProtobufSerializer.Tests
{
    [PayloadTable(typeof(IDefault), PayloadTableKind.Request)]
    public static class IDefault_PayloadTable
    {
        public static Type[,] GetPayloadTypes()
        {
            return new Type[,] {
                { typeof(Call_Invoke), null },
                { typeof(CallWithDefault_Invoke), null },
            };
        }

        [ProtoContract, TypeAlias]
        public class Call_Invoke
            : IInterfacedPayload, IAsyncInvokable
        {
            [ProtoMember(1)] public System.Int32 a;
            [ProtoMember(2)] public System.Int32 b;
            [ProtoMember(3)] public System.String c;

            public Type GetInterfaceType()
            {
                return typeof(IDefault);
            }

            public async Task<IValueGetable> InvokeAsync(object __target)
            {
                await ((IDefault)__target).Call(a, b, c);
                return null;
            }
        }

        [ProtoContract, TypeAlias]
        public class CallWithDefault_Invoke
            : IInterfacedPayload, IAsyncInvokable
        {
            [ProtoMember(1), DefaultValue(1)] public System.Int32 a = 1;
            [ProtoMember(2), DefaultValue(2)] public System.Int32 b = 2;
            [ProtoMember(3), DefaultValue("Test")] public System.String c = "Test";

            public Type GetInterfaceType()
            {
                return typeof(IDefault);
            }

            public async Task<IValueGetable> InvokeAsync(object __target)
            {
                await ((IDefault)__target).CallWithDefault(a, b, c);
                return null;
            }
        }
    }

    public interface IDefault_NoReply
    {
        void Call(System.Int32 a, System.Int32 b, System.String c);
        void CallWithDefault(System.Int32 a = 1, System.Int32 b = 2, System.String c = "Test");
    }

    public class DefaultRef : InterfacedActorRef, IDefault, IDefault_NoReply
    {
        public DefaultRef() : base(null)
        {
        }

        public DefaultRef(IActorRef actor) : base(actor)
        {
        }

        public DefaultRef(IActorRef actor, IRequestWaiter requestWaiter, TimeSpan? timeout = null) : base(actor, requestWaiter, timeout)
        {
        }

        public IDefault_NoReply WithNoReply()
        {
            return this;
        }

        public DefaultRef WithRequestWaiter(IRequestWaiter requestWaiter)
        {
            return new DefaultRef(Actor, requestWaiter, Timeout);
        }

        public DefaultRef WithTimeout(TimeSpan? timeout)
        {
            return new DefaultRef(Actor, RequestWaiter, timeout);
        }

        public Task Call(System.Int32 a, System.Int32 b, System.String c)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IDefault_PayloadTable.Call_Invoke { a = a, b = b, c = c }
            };
            return SendRequestAndWait(requestMessage);
        }

        public Task CallWithDefault(System.Int32 a = 1, System.Int32 b = 2, System.String c = "Test")
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IDefault_PayloadTable.CallWithDefault_Invoke { a = a, b = b, c = c }
            };
            return SendRequestAndWait(requestMessage);
        }

        void IDefault_NoReply.Call(System.Int32 a, System.Int32 b, System.String c)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IDefault_PayloadTable.Call_Invoke { a = a, b = b, c = c }
            };
            SendRequest(requestMessage);
        }

        void IDefault_NoReply.CallWithDefault(System.Int32 a, System.Int32 b, System.String c)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IDefault_PayloadTable.CallWithDefault_Invoke { a = a, b = b, c = c }
            };
            SendRequest(requestMessage);
        }
    }

    [ProtoContract]
    public class SurrogateForIDefault
    {
        [ProtoMember(1)] public IActorRef Actor;

        [ProtoConverter]
        public static SurrogateForIDefault Convert(IDefault value)
        {
            if (value == null) return null;
            return new SurrogateForIDefault { Actor = ((DefaultRef)value).Actor };
        }

        [ProtoConverter]
        public static IDefault Convert(SurrogateForIDefault value)
        {
            if (value == null) return null;
            return new DefaultRef(value.Actor);
        }
    }

    [AlternativeInterface(typeof(IDefault))]
    public interface IDefaultSync : IInterfacedActor
    {
        void Call(System.Int32 a, System.Int32 b, System.String c);
        void CallWithDefault(System.Int32 a = 1, System.Int32 b = 2, System.String c = "Test");
    }
}

#endregion