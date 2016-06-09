using System;
using Akka.Interfaced;
using SlimHttp.Interface;

namespace SlimHttp.Program.Server
{
    [ResponsiveException(typeof(ArgumentException))]
    public class GreetingActor : InterfacedActor, IGreeterSync
    {
        private int _count;

        string IGreeterSync.Greet(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            _count += 1;
            return $"Hello {name}!";
        }

        int IGreeterSync.GetCount()
        {
            return _count;
        }
    }
}
