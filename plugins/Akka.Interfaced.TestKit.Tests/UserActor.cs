using System.Threading.Tasks;

namespace Akka.Interfaced.TestKit.Tests
{
    public class UserActor : InterfacedActor<UserActor>, IUser
    {
        private readonly string _id;
        private readonly UserObserver _observer;

        public UserActor(string id, UserObserver observer)
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
