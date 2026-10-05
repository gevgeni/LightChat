using LightChat.Core.Features.Chats.AddChatMember;
using MediatR;

namespace LightChat.Core.Features.Notifications.SubscribePush
{
    public record SubscribePushCommand(Guid UserId, string Endpoint, string P256dh, string Auth) : IRequest<SubscribePushDto>;
    public record SubscribePushDto(Guid UserId, string Endpoint);
}