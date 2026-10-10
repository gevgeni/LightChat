namespace LightChat.Core.Interfaces
{
    public interface IUserStatusManager
    {
        void SetOnline(Guid userId, string connectionId);
        void RemoveConnection(string connectionId, out Guid? userId, out bool isFullyOffline);
        IEnumerable<Guid> GetOnlineUsers();
        bool IsUserOnline(Guid userId);
        Task<bool> IsUserOnlineAsync(Guid userId);
        void ClearAllStatuses();
    }
}