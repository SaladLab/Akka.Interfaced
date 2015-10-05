using System;
using System.Reflection;

namespace Akka.Interfaced
{
    // Filter Factory

    public interface IFilterFactory
    {
    }

    public interface IFilterPerClassFactory : IFilterFactory
    {
        void Setup(Type actorType);
        IFilter CreateInstance();
    }

    public interface IFilterPerClassMethodFactory : IFilterFactory
    {
        void Setup(Type actorType, MethodInfo method);
        IFilter CreateInstance();
    }

    public interface IFilterPerInstanceFactory : IFilterFactory
    {
        void Setup(Type actorType);
        IFilter CreateInstance(object actor);
    }

    public interface IFilterPerInstanceMethodFactory : IFilterFactory
    {
        void Setup(Type actorType, MethodInfo method);
        IFilter CreateInstance(object actor);
    }

    public interface IFilterPerInvokeFactory : IFilterFactory
    {
        void Setup(Type actorType, MethodInfo method);
        IFilter CreateInstance(object actor, RequestMessage request);
    }

    // Filter Accessor

    public interface IFilterPerInstanceProvider
    {
        IFilter GetFilter(int index);
    }

    public delegate IFilter FilterAccessor(
        IFilterPerInstanceProvider perInstanceFilterProvider,
        IFilter[] filterPerInvokes);
}
