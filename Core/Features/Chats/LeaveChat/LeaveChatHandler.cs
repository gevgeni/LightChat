using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;
using MediatR;

namespace LightChat.Core.Features.Chats.LeaveChat
{
    public class LeaveChatHandler : IRequestHandler<LeaveChatCommand>
    {
        private readonly IChatRepository _chatRepository;
        private readonly ICacheInvalidator _cacheInvalidator;

        public LeaveChatHandler(IChatRepository chatRepository, ICacheInvalidator cacheInvalidator)
        {
            _chatRepository = chatRepository;
            _cacheInvalidator = cacheInvalidator;
        }

        public async Task Handle(LeaveChatCommand request, CancellationToken cancellationToken)
        {
            var isMember = await _chatRepository.IsMemberAsync(request.ChatId, request.UserId);

            if (!isMember)
                throw new UnauthorizedAccessException("Вы не состоите в этом чате.");

            await _chatRepository.RemoveMemberAsync(request.ChatId, request.UserId);

            await _cacheInvalidator.InvalidateChatMembersAsync(request.ChatId);
            await _cacheInvalidator.InvalidateUserChatsAsync(request.UserId);
        }
    }
}