using System.Collections.Generic;
using System.Threading.Tasks;

namespace Akka.Interfaced.Persistence.Tests
{
    public interface INotepad : IInterfacedActor
    {
        Task Clear();
        Task Write(string message);
        Task FlushSnapshot();
        Task<IList<string>> GetDocument();
    }
}
