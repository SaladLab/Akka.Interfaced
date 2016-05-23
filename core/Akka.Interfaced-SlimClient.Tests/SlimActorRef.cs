using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Akka.Interfaced.SlimClient.Tests
{
    public class SlimActorRef
    {
        [Fact]
        public void Call_ToActorRef_TransferedViaRequestWaiter()
        {
            var requestWaiter = new TestRequestWaiter();
            var a = new BasicRef(null, requestWaiter, null);
            a.Call();

            Assert.Equal(1, requestWaiter.Requests.Count);
            Assert.IsType<IBasic_PayloadTable.Call_Invoke>(requestWaiter.Requests[0].InvokePayload);
        }

        [Fact]
        public void CallWithParameter_ToActorRef_TransferedViaRequestWaiter()
        {
            var requestWaiter = new TestRequestWaiter();
            var a = new BasicRef(null, requestWaiter, null);
            a.CallWithParameter(1);

            Assert.Equal(1, requestWaiter.Requests.Count);
            var payload = (IBasic_PayloadTable.CallWithParameter_Invoke)requestWaiter.Requests[0].InvokePayload;
            Assert.NotNull(payload);
            Assert.Equal(1, payload.value);
        }
    }
}
