using Xunit;

namespace Akka.Interfaced
{
    public class InterfacedMessageBuilderTest
    {
        [Fact]
        public void BuildRequestMessage()
        {
            var message = InterfacedMessageBuilder.Request<IBasic>(x => x.CallWithParameter(10));
            var payload = message.InvokePayload as IBasic_PayloadTable.CallWithParameter_Invoke;
            Assert.NotNull(payload);
            Assert.Equal(10, payload.value);
        }

        [Fact]
        public void BuildNotificationMessage()
        {
            var message = InterfacedMessageBuilder.Notification<ISubject2Observer>(x => x.Event("A"));
            var payload = message.InvokePayload as ISubject2Observer_PayloadTable.Event_Invoke;
            Assert.NotNull(payload);
            Assert.Equal("A", payload.eventName);
        }
    }
}
