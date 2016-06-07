using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Akka.Actor;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class MessageFilterTest : TestKit.Xunit2.TestKit
    {
        public MessageFilterTest(ITestOutputHelper output)
            : base(output: output)
        {
        }
    }
}
