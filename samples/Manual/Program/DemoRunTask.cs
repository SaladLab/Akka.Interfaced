using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;

namespace Manual
{
    public class DemoRunTask
    {
        private ActorSystem _system;

        public DemoRunTask(ActorSystem system, string[] args)
        {
            _system = system;
        }

        public class MyActor : InterfacedActor
        {
            [MessageHandler]
            private void Handle(string message)
            {
                // schedule syncrhnous task
                RunTask(() =>
                {
                    Console.WriteLine($"RunTask: {message}");
                });
            }
        }

        private async Task DemoRunSyncTask()
        {
            var actor = _system.ActorOf<MyActor>();
            actor.Tell("Test");
            await Task.Delay(100);
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);
        }

        public class MyActor2 : InterfacedActor
        {
            [MessageHandler]
            private void Handle(string message)
            {
                // schedule asynchronous task
                RunTask(async () =>
                {
                    Console.WriteLine($"RunAsyncTask: {message}");
                    await Task.Delay(10);
                    Console.WriteLine($"RunAsyncTask: {message} End");
                });
            }
        }

        private async Task DemoRunAsyncTask()
        {
            var actor = _system.ActorOf<MyActor2>();
            actor.Tell("Test");
            await Task.Delay(100);
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);
        }

        public class MyActor3 : InterfacedActor
        {
            [MessageHandler]
            private void Handle(string message)
            {
                // schedule two asynchronos reentrant tasks
                for (int i = 0; i < 2; i++)
                {
                    var captured = i;
                    RunTask(async () =>
                    {
                        Console.WriteLine($"RunAsyncReentrantTask: {message} {captured}");
                        await Task.Delay(10);
                        Console.WriteLine($"RunAsyncReentrantTask: {message} {captured} End");
                    }, isReentrant: true);
                }
            }
        }

        private async Task DemoRunAsyncReentrantTask()
        {
            var actor = _system.ActorOf<MyActor3>();
            actor.Tell("Test");
            await Task.Delay(100);
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);
        }
    }
}
