namespace Akka.Interfaced
{
    /// <summary>
    ///   Provides a mechanism for invoking handler of message
    /// </summary>
    public interface IInvokable
    {
        /// <summary>
        ///   Invoke handler of message
        /// </summary>
        /// <param name="target">Target object. Most of time this is an actor.</param>
        void Invoke(object target);
    }
}
