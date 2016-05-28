using System.Threading.Tasks;

namespace Akka.Interfaced.TestKit.Tests
{
    public class UserActor : InterfacedActor, IUser
    {
        private readonly string _id;
        private readonly IUserObserver _observer;

        public UserActor(string id, IUserObserver observer)
        {
            _id = id;
            _observer = observer;
        }

        Task<string> IUser.GetId()
        {
            return Task.FromResult(_id);
        }

        Task IUser.Say(string message)
        {
            _observer?.Say(message);
            return Task.FromResult(0);
        }
    }
}
