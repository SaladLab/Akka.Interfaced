using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced.SlimServer;

namespace Akka.Interfaced.TestKit.Tests
{
    public class UserLoginActor : InterfacedActor, IUserLogin
    {
        private readonly ActorBoundChannelRef _actorBoundChannel;

        public UserLoginActor(IActorRef actorBoundChannel)
        {
            _actorBoundChannel = actorBoundChannel.Cast<ActorBoundChannelRef>().WithRequestWaiter(this);
        }

        public async Task<IUser> Login(string id, string password, IUserObserver observer)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            // Check account

            var ok = CheckAccount(id, password);
            if (ok == false)
                throw new InvalidCredentialException();

            // Make UserActor and bind it

            var user = Context.System.InterfacedActorOf(Props.Create(() => new UserActor(id, observer))).Cast<UserRef>();
            return (await _actorBoundChannel.BindActor(user)).Cast<UserRef>();
        }

        private bool CheckAccount(string id, string password)
        {
            return id == password;
        }
    }
}
