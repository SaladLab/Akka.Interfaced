using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Akka.Actor;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class ObserverHandleTest : TestKit.Xunit2.TestKit
    {
        public ObserverHandleTest(ITestOutputHelper output)
            : base(output: output)
        {
        }
    }
}
