using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;

namespace Manual
{
    public class DemoFilter
    {
        private ActorSystem _system;

        public DemoFilter(ActorSystem system, string[] args)
        {
            _system = system;
        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class LogAttribute : Attribute, IFilterPerClassFactory, IPreRequestFilter, IPostRequestFilter
        {
            void IFilterPerClassFactory.Setup(Type actorType) { }

            IFilter IFilterPerClassFactory.CreateInstance() => this;

            int IFilter.Order => 1;

            void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
            {
                if (context.Handled)
                    return;

                Console.Write("Request: " + context.Request.InvokePayload.GetType().Name);
            }

            void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
            {
                if (context.Response.Exception != null)
                    Console.WriteLine(" -> ! " + context.Response.Exception.GetType().Name);
                else if (context.Response.ReturnPayload != null)
                    Console.WriteLine(" -> " + context.Response.ReturnPayload.Value);
                else
                    Console.WriteLine(" -> Done" + context.Response.ReturnPayload.Value);
            }
        }

        public interface IAuthorizable
        {
            bool Authorized { get; }
        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class AuthorizedAttribute : Attribute, IFilterPerClassFactory, IPreRequestFilter
        {
            void IFilterPerClassFactory.Setup(Type actorType) { }

            IFilter IFilterPerClassFactory.CreateInstance() => this;

            int IFilter.Order => 2;

            void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
            {
                if (context.Handled)
                    return;

                var actor = (IAuthorizable)context.Actor;
                if (actor == null || actor.Authorized == false)
                {
                    context.Response = new ResponseMessage
                    {
                        RequestId = context.Request.RequestId,
                        Exception = new InvalidOperationException("Not enough permission.")
                    };
                    return;
                }
            }
        }

        [Log, ResponsiveException(typeof(ArgumentException))]
        public class MyActor : InterfacedActor, IGreeterSync, IAuthorizable
        {
            private int _count;

            public int Permission { get; }

            bool IAuthorizable.Authorized => Permission > 0;

            public MyActor(int permission)
            {
                Permission = permission;
            }

            string IGreeterSync.Greet(string name)
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException(nameof(name));

                _count += 1;
                return $"Hello {name}!";
            }

            [Authorized]
            int IGreeterSync.GetCount()
            {
                return _count;
            }
        }

        private async Task DoWork(int permission)
        {
            Console.WriteLine($"- Work with permission={permission}");

            var greeter = _system.ActorOf(Props.Create(() => new MyActor(permission))).Cast<GreeterRef>();

            try
            {
                await greeter.Greet("Hello");
                await greeter.Greet("Actor");
                await greeter.GetCount();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private async Task DemoBasic()
        {
            await DoWork(0);
            await DoWork(1);
        }
    }
}
