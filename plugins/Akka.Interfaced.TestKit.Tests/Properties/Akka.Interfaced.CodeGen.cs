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

#region Akka.Interfaced.TestKit.Tests.IUser

namespace Akka.Interfaced.TestKit.Tests
{
    [PayloadTable(typeof(IUser), PayloadTableKind.Request)]
    public static class IUser_PayloadTable
    {
        public static Type[,] GetPayloadTypes()
        {
            return new Type[,] {
                { typeof(GetId_Invoke), typeof(GetId_Return) },
                { typeof(Say_Invoke), null },
            };
        }

        public class GetId_Invoke
            : IInterfacedPayload, IAsyncInvokable
        {
            public Type GetInterfaceType()
            {
                return typeof(IUser);
            }

            public async Task<IValueGetable> InvokeAsync(object __target)
            {
                var __v = await ((IUser)__target).GetId();
                return (IValueGetable)(new GetId_Return { v = __v });
            }
        }

        public class GetId_Return
            : IInterfacedPayload, IValueGetable
        {
            public System.String v;

            public Type GetInterfaceType()
            {
                return typeof(IUser);
            }

            public object Value
            {
                get { return v; }
            }
        }

        public class Say_Invoke
            : IInterfacedPayload, IAsyncInvokable
        {
            public System.String message;

            public Type GetInterfaceType()
            {
                return typeof(IUser);
            }

            public async Task<IValueGetable> InvokeAsync(object __target)
            {
                await ((IUser)__target).Say(message);
                return null;
            }
        }
    }

    public interface IUser_NoReply
    {
        void GetId();
        void Say(System.String message);
    }

    public class UserRef : InterfacedActorRef, IUser, IUser_NoReply
    {
        public UserRef() : base(null)
        {
        }

        public UserRef(IActorRef actor) : base(actor)
        {
        }

        public UserRef(IActorRef actor, IRequestWaiter requestWaiter, TimeSpan? timeout = null) : base(actor, requestWaiter, timeout)
        {
        }

        public IUser_NoReply WithNoReply()
        {
            return this;
        }

        public UserRef WithRequestWaiter(IRequestWaiter requestWaiter)
        {
            return new UserRef(Actor, requestWaiter, Timeout);
        }

        public UserRef WithTimeout(TimeSpan? timeout)
        {
            return new UserRef(Actor, RequestWaiter, timeout);
        }

        public Task<System.String> GetId()
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUser_PayloadTable.GetId_Invoke {  }
            };
            return SendRequestAndReceive<System.String>(requestMessage);
        }

        public Task Say(System.String message)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUser_PayloadTable.Say_Invoke { message = message }
            };
            return SendRequestAndWait(requestMessage);
        }

        void IUser_NoReply.GetId()
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUser_PayloadTable.GetId_Invoke {  }
            };
            SendRequest(requestMessage);
        }

        void IUser_NoReply.Say(System.String message)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUser_PayloadTable.Say_Invoke { message = message }
            };
            SendRequest(requestMessage);
        }
    }

    [AlternativeInterface(typeof(IUser))]
    public interface IUserSync : IInterfacedActor
    {
        System.String GetId();
        void Say(System.String message);
    }
}

#endregion
#region Akka.Interfaced.TestKit.Tests.IUserLogin

namespace Akka.Interfaced.TestKit.Tests
{
    [PayloadTable(typeof(IUserLogin), PayloadTableKind.Request)]
    public static class IUserLogin_PayloadTable
    {
        public static Type[,] GetPayloadTypes()
        {
            return new Type[,] {
                { typeof(Login_Invoke), typeof(Login_Return) },
            };
        }

        public class Login_Invoke
            : IInterfacedPayload, IAsyncInvokable, IPayloadObserverUpdatable
        {
            public System.String id;
            public System.String password;
            public Akka.Interfaced.TestKit.Tests.IUserObserver observer;

            public Type GetInterfaceType()
            {
                return typeof(IUserLogin);
            }

            public async Task<IValueGetable> InvokeAsync(object __target)
            {
                var __v = await ((IUserLogin)__target).Login(id, password, observer);
                return (IValueGetable)(new Login_Return { v = __v });
            }

            void IPayloadObserverUpdatable.Update(Action<IInterfacedObserver> updater)
            {
                if (observer != null)
                {
                    updater(observer);
                }
            }
        }

        public class Login_Return
            : IInterfacedPayload, IValueGetable, IPayloadActorRefUpdatable
        {
            public Akka.Interfaced.TestKit.Tests.IUser v;

            public Type GetInterfaceType()
            {
                return typeof(IUserLogin);
            }

            public object Value
            {
                get { return v; }
            }

            void IPayloadActorRefUpdatable.Update(Action<object> updater)
            {
                if (v != null)
                {
                    updater(v); 
                }
            }
        }
    }

    public interface IUserLogin_NoReply
    {
        void Login(System.String id, System.String password, Akka.Interfaced.TestKit.Tests.IUserObserver observer);
    }

    public class UserLoginRef : InterfacedActorRef, IUserLogin, IUserLogin_NoReply
    {
        public UserLoginRef() : base(null)
        {
        }

        public UserLoginRef(IActorRef actor) : base(actor)
        {
        }

        public UserLoginRef(IActorRef actor, IRequestWaiter requestWaiter, TimeSpan? timeout = null) : base(actor, requestWaiter, timeout)
        {
        }

        public IUserLogin_NoReply WithNoReply()
        {
            return this;
        }

        public UserLoginRef WithRequestWaiter(IRequestWaiter requestWaiter)
        {
            return new UserLoginRef(Actor, requestWaiter, Timeout);
        }

        public UserLoginRef WithTimeout(TimeSpan? timeout)
        {
            return new UserLoginRef(Actor, RequestWaiter, timeout);
        }

        public Task<Akka.Interfaced.TestKit.Tests.IUser> Login(System.String id, System.String password, Akka.Interfaced.TestKit.Tests.IUserObserver observer)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUserLogin_PayloadTable.Login_Invoke { id = id, password = password, observer = (UserObserver)observer }
            };
            return SendRequestAndReceive<Akka.Interfaced.TestKit.Tests.IUser>(requestMessage);
        }

        void IUserLogin_NoReply.Login(System.String id, System.String password, Akka.Interfaced.TestKit.Tests.IUserObserver observer)
        {
            var requestMessage = new RequestMessage {
                InvokePayload = new IUserLogin_PayloadTable.Login_Invoke { id = id, password = password, observer = (UserObserver)observer }
            };
            SendRequest(requestMessage);
        }
    }

    [AlternativeInterface(typeof(IUserLogin))]
    public interface IUserLoginSync : IInterfacedActor
    {
        Akka.Interfaced.TestKit.Tests.IUser Login(System.String id, System.String password, Akka.Interfaced.TestKit.Tests.IUserObserver observer);
    }
}

#endregion
#region Akka.Interfaced.TestKit.Tests.IUserObserver

namespace Akka.Interfaced.TestKit.Tests
{
    [PayloadTable(typeof(IUserObserver), PayloadTableKind.Notification)]
    public static class IUserObserver_PayloadTable
    {
        public static Type[] GetPayloadTypes()
        {
            return new Type[] {
                typeof(Say_Invoke),
            };
        }

        public class Say_Invoke : IInterfacedPayload, IInvokable
        {
            public System.String message;

            public Type GetInterfaceType()
            {
                return typeof(IUserObserver);
            }

            public void Invoke(object __target)
            {
                ((IUserObserver)__target).Say(message);
            }
        }
    }

    public class UserObserver : InterfacedObserver, IUserObserver
    {
        public UserObserver()
            : base(null, 0)
        {
        }

        public UserObserver(INotificationChannel channel, int observerId = 0)
            : base(channel, observerId)
        {
        }

        public void Say(System.String message)
        {
            var payload = new IUserObserver_PayloadTable.Say_Invoke { message = message };
            Notify(payload);
        }
    }

    [AlternativeInterface(typeof(IUserObserver))]
    public interface IUserObserverAsync : IInterfacedObserver
    {
        Task Say(System.String message);
    }
}

#endregion