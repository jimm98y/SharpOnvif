namespace SharpOnvifServer.Dispatch
{
    /// <summary>
    /// A service that knows what routes actions to it.
    /// </summary>
    /// <remarks>
    /// Implemented by every generated service base, and the reason routing needs no reflection: a
    /// dispatcher is a static of the base class, which nothing derived from it can be asked for
    /// through an ordinary interface. Asking through a type parameter can, and asking at compile
    /// time means a type that is not a generated service is a build error rather than a surprise
    /// when the endpoint is mapped.
    /// </remarks>
    public interface IDispatchedService
    {
        /// <summary>Routes a SOAP action to one of this service's operations.</summary>
        static abstract ServiceDispatcher Dispatcher { get; }
    }
}
