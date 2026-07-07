using StackExchange.Redis;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace LightChat.Web.Services
{
    public class UserStatusManager : IUserStatusManager
    {
        private readonly IDatabase _redisDb;

        private const string GlobalConnectionKey = "chat:connections";
        private const string UserConnectionsPrefix = "chat:user:{0}:connections";

        public UserStatusManager(IConnectionMultiplexer redis)
        {
            _redisDb = redis.GetDatabase();
        }

        public void SetOnline(Guid userId, string connectionId)
        {
            _redisDb.HashSet(GlobalConnectionKey, connectionId, userId.ToString());

            var userKey = string.Format(UserConnectionsPrefix, userId);
            _redisDb.SetAdd(userKey, connectionId);
        }

        public void RemoveConnection(string connectionId, out Guid? userId, out bool isFullyOffline)
        {
            userId = null;
            isFullyOffline = false;

            var userIdValue = _redisDb.HashGet(GlobalConnectionKey, connectionId);

            if (userIdValue.HasValue && Guid.TryParse(userIdValue, out var uid))
            {
                userId = uid;

                _redisDb.HashDelete(GlobalConnectionKey, connectionId);

                var userKey = string.Format(UserConnectionsPrefix, uid);
                _redisDb.SetRemove(userKey, connectionId);

                var remainingConnections = _redisDb.SetLength(userKey);
                if (remainingConnections == 0)
                    isFullyOffline = true;
            }
        }

        public IEnumerable<Guid> GetOnlineUsers()
        {
            return [];
        }

        public bool IsUserOnline(Guid userId)
        {
            var userKey = string.Format(UserConnectionsPrefix, userId);
            return _redisDb.SetLength(userKey) > 0;
        }

        public void ClearAllStatuses()
        {
            _redisDb.KeyDelete(GlobalConnectionKey);

            var endpoints = _redisDb.Multiplexer.GetEndPoints();

            if (endpoints.Length > 0)
            {
                var server = _redisDb.Multiplexer.GetServer(endpoints[0]);

                var keys = server.Keys(pattern: "chat:user:*:connections").ToArray();
                if (keys.Length > 0)
                    _redisDb.KeyDelete(keys);
            }
        }
    }
}
