namespace LightChat.Core.Interfaces;

public interface ICacheInvalidator
{
    Task InvalidateUserChatsAsync(params Guid[] userIds);
    Task InvalidateChatMembersAsync(Guid chatId);
    Task InvalidateAllUsersListAsync();
    Task InvalidateUserByIdAsync(Guid userId);
}