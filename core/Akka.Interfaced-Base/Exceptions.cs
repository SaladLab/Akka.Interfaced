﻿using System;

namespace Akka.Interfaced
{
    /// <summary>
    /// This exception provides the base for all Akka.Interfaced specific exceptions within the system.
    /// </summary>
    public abstract class AkkaInterfacedException : Exception
    {
        protected AkkaInterfacedException()
        {
        }

        protected AkkaInterfacedException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when the actor which have a request is not found.
    /// </summary>
    public class RequestTargetException : AkkaInterfacedException
    {
        public RequestTargetException()
        {
        }

        public RequestTargetException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when there is an unhandled exception in processing a request.
    /// </summary>
    public class RequestFaultException : AkkaInterfacedException
    {
        public RequestFaultException()
        {
        }

        public RequestFaultException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when the actor is stopped in processing a request.
    /// </summary>
    public class RequestHaltException : AkkaInterfacedException
    {
        public RequestHaltException()
        {
        }

        public RequestHaltException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}