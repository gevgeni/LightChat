using MediatR;

namespace LightChat.Core.Features.Chats
{
    public record AddChatMemberCommand(Guid ChatId, Guid TargetUserId, Guid CurrentUserId) : IRequest<AddMemberDto>;
    public record AddMemberDto(Guid ChatId, string ChatName, bool IsDirect, Guid TargetUserId);
}
