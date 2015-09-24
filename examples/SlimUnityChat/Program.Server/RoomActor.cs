using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Akka.Interfaced;
using SlimUnityChat.Interface;
using Common.Logging;
using Newtonsoft.Json;

namespace SlimUnityChat.Program.Server
{
    public class RoomActor : InterfacedActor<RoomActor>, IRoom, IOccupant
    {
        private class UserData
        {
            public UserRef UserActor;
            public RoomObserver Observer;
        }

        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private string _name;
        private Dictionary<string, UserData> _userMap;
        private bool _removed;
        private List<ChatItem> _history;
        private static readonly int HistoryMax = 100;

        public RoomActor(ClusterNodeContext clusterContext, string name)
        {
            _logger = LogManager.GetLogger(string.Format("RoomActor({0})", name));
            _clusterContext = clusterContext;
            _name = name;
            _userMap = new Dictionary<string, UserData>();
        }

        protected static MessageHandler OnBuildHandler(MessageHandler handler, MethodInfo method)
        {
            return LogHandlerBuilder.BuildHandler(self => self._logger, handler, method, true);
        }

        private void NotifyToAllObservers(Action<RoomObserver> notifyAction)
        {
            foreach (var item in _userMap)
            {
                if (item.Value.Observer != null)
                    notifyAction(item.Value.Observer);
            }
        }

        protected override async Task OnPreStart()
        {
            await LoadAsync();
        }

        protected override async Task OnPreStop()
        {
            await SaveAsync();
        }

        private async Task LoadAsync()
        {
            List<ChatItem> history = null;

            var data = await RedisStorage.Db.HashGetAsync("RoomHistory", _name);
            if (data.HasValue)
            {
                try
                {
                    history = JsonConvert.DeserializeObject<List<ChatItem>>(data.ToString());
                }
                catch (Exception e)
                {
                    _logger.ErrorFormat("Error occured in loading room({0})", e, _name);
                }
            }

            _history = history ?? new List<ChatItem>();
        }

        private async Task SaveAsync()
        {
            try
            {
                var historyJson = JsonConvert.SerializeObject(_history);
                await RedisStorage.Db.HashSetAsync("RoomHistory", _name, historyJson);
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Error occured in saving room({0})", e, _name);
            }
        }

        async Task<RoomInfo> IRoom.Enter(string userId, IRoomObserver observer)
        {
            if (_removed)
                throw new ResultException(ResultCodeType.RoomRemoved);

            if (_userMap.ContainsKey(userId))
                throw new ResultException(ResultCodeType.NeedToBeOutOfRoom);

            NotifyToAllObservers(o => o.Enter(userId));

            _userMap[userId] = new UserData
            {
                UserActor = new UserRef(Sender, this, null),
                Observer = (RoomObserver)observer
            };

            return new RoomInfo
            {
                Name = _name,
                Users = _userMap.Keys.ToList(),
                History = _history
            };
        }

        async Task IRoom.Exit(string userId)
        {
            if (_userMap.ContainsKey(userId) == false)
                throw new ResultException(ResultCodeType.NeedToBeInRoom);

            _userMap.Remove(userId);

            NotifyToAllObservers(o => o.Exit(userId));

            if (_userMap.Count == 0)
            {
                // Leave 가 되어 RoomDirectory 에서 삭제가 되어야 하기 직전에
                // 유저가 들어올 수 있어서 이를 flag 로 가드.

                _removed = true;

                _clusterContext.RoomDirectory.WithNoReply().RemoveRoom(_name);
            }
        }

        async Task IOccupant.Say(string msg, string senderUserId)
        {
            if (_userMap.ContainsKey(senderUserId) == false)
                throw new ResultException(ResultCodeType.NeedToBeInRoom);

            var chatItem = new ChatItem { Time = DateTime.UtcNow, UserId = senderUserId, Message = msg };
            _history.Add(chatItem);
            if (_history.Count > HistoryMax)
                _history.RemoveRange(0, _history.Count - HistoryMax);

            NotifyToAllObservers(o => o.Say(chatItem));
        }

        Task<List<ChatItem>> IOccupant.GetHistory()
        {
            return Task.FromResult(_history);
        }

        async Task IOccupant.Invite(string targetUserId, string senderUserId)
        {
            if (targetUserId == senderUserId)
                throw new ResultException(ResultCodeType.UserNotMyself);

            if (_userMap.ContainsKey(targetUserId))
                throw new ResultException(ResultCodeType.UserAlreadyHere);

            var targetUser = await _clusterContext.UserDirectory.GetUser(targetUserId);
            if (targetUser == null)
                throw new ResultException(ResultCodeType.UserNotOnline);

            // TODO: not a good way.. is there a type-safe way?
            var targetUserMessaging = new UserMessasingRef(((UserRef)targetUser).Actor, null, null);
            targetUserMessaging.Invite(senderUserId, _name);
        }
    }
}
