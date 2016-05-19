using System.Threading.Tasks;
using Akka.Interfaced;

namespace CodeVerifier.Tests
{
    public interface IFoo : IInterfacedActor
    {
        Task Hello(int delta);
        Task<int> Hello();
    }

    public interface IBar : IInterfacedActor
    {
        Task Bye(int delta);
        Task<int> Bye();
    }

    public interface IBar2 : IInterfacedActor
    {
        Task Bye(int delta);
        Task<string> Bye();
    }

    public class ExtendedClass_MethodCompleted : InterfacedActor, IExtendedInterface<IFoo>
    {
        [ExtendedHandler]
        private void Hello(int delta)
        {
        }

        [ExtendedHandler]
        private int Hello()
        {
            return 0;
        }
    }

    public class ExtendedClass_MethodMissing : InterfacedActor, IExtendedInterface<IFoo>
    {
        [ExtendedHandler]
        private void Hello(int delta)
        {
        }
    }

    public class ExtendedClass_MethodRedundant : InterfacedActor, IExtendedInterface<IFoo>
    {
        [ExtendedHandler]
        private void Hello(int delta)
        {
        }

        [ExtendedHandler]
        private int Hello()
        {
            return 0;
        }

        [ExtendedHandler]
        private void Bye(int delta)
        {
        }
    }

    public class ExtendedClass_ExplicitMethodCompleted : InterfacedActor, IExtendedInterface<IBar, IBar2>
    {
        [ExtendedHandler(typeof(IBar))]
        private void Bye(int delta)
        {
        }

        [ExtendedHandler(typeof(IBar))]
        private int Bye()
        {
            return 0;
        }

        [ExtendedHandler(typeof(IBar2), "Bye")]
        private void Bye2(int delta)
        {
        }

        [ExtendedHandler(typeof(IBar2), "Bye")]
        private int Bye2()
        {
            return 0;
        }
    }
}
