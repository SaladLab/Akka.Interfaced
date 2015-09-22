using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public class NotificationMessage
    {
        public int ObserverId;
        public IInvokable Message;
    }
}
