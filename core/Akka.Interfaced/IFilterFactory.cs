using System;
using System.Reflection;

namespace Akka.Interfaced
{
    public interface IFilterFactory
    {
    }

    public interface IFilterPerClassFactory : IFilterFactory
    {
        IFilter CreateInstance(Type actorType);
    }

    public interface IFilterPerClassMethodFactory : IFilterFactory
    {
        IFilter CreateInstance(Type actorType, MethodInfo method);
    }

    // TODO: IMPLEMENT!
    /*
    public interface IFilterPerInstanceFactory : IFilterFactory
    {
        IFilter CreateInstance(Type actorType, object actor);
    }

    public interface IFilterPerInstanceMethodFactory : IFilterFactory
    {
        IFilter CreateInstance(Type actorType, object actor, MethodInfo method);
    }

    public interface IFilterPerInvokeFactory : IFilterFactory
    {
        IFilter CreateInstance(Type actorType, object actor, RequestMessage request);
    }
    */
}
