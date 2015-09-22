using System;

namespace Akka.Interfaced
{
    /// <summary>
    ///   Define a interfaced message class.
    /// </summary>
    public interface IInterfacedMessage
    {
        /// <summary>
        ///   Return owner interface type 
        /// </summary>
        /// <returns>
        ///   Owner interface type
        /// </returns>
        Type GetInterfaceType();
    }
}
