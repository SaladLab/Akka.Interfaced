using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Akka.Actor;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class TaskRunTest : TestKit.Xunit2.TestKit
    {
        public TaskRunTest(ITestOutputHelper output)
            : base(output: output)
        {
        }
    }
}
