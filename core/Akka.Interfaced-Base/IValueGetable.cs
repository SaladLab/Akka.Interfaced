namespace Akka.Interfaced
{
    /// <summary>
    ///   Provides a mechanism for returning value.
    /// </summary>
    /// <remarks>
    ///   Reasons to use IValueGetable instead of plain T
    ///   - Differentiate return of void and return of null of T.
    ///   - Envelop of plain T as a message
    /// </remarks>
    public interface IValueGetable
    {
        /// <summary>
        ///   Return value
        /// </summary>
        object Value { get; }
    }
}
