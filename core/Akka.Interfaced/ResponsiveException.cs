using System;
using System.Linq;

namespace Akka.Interfaced
{
    /// <summary>
    /// This exception is thrown when a request handler encounters an exception
    /// to be expected to forward to a requester rather than the supervior of handling actor.
    /// </summary>
    public class ResponsiveException : Exception
    {
        public ResponsiveException(Exception innerException)
            : base(null, innerException)
        {
            if (innerException == null)
            {
                throw new ArgumentNullException(nameof(InnerException));
            }
        }
    }
}
