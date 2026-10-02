using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;
using MediatR;

namespace LightChat.Core.Features.Chats.DeleteChat
{
    public class DeleteChatHandler : IRequestHandler<DeleteChatCommand>
    {
        private readonly IChatRepository _chatRepository;
        private readonly ICacheInvalidator _cacheInvalidator;
        public DeleteChatHandler(IChatRepository chatRepository, ICacheInvalidator cacheInvalidator)
        {
            _chatRepository = chatRepository;
            _cacheInvalidator = cacheInvalidator;
        }

        public async Task Handle(DeleteChatCommand request, CancellationToken cancellationToken)
        {
            var chat = await _chatRepository.GetByIdAsync(request.ChatId)
                ?? throw new KeyNotFoundException("Чат не найден.");

            var isMember = await _chatRepository.IsMemberAsync(request.ChatId, request.UserId);
            if (!isMember)
                throw new UnauthorizedAccessException("Вы не являетесь участником этого чата.");

            if (chat.CreatorId != request.UserId)
                throw new UnauthorizedAccessException("Вы не являетесь создателем этого чата.");

            var members = await _chatRepository.GetMembersAsync(request.ChatId);
            var memberIds = members.Select(m => m.Id).ToArray();

            await _chatRepository.DeleteChatAsync(request.ChatId);

            await _cacheInvalidator.InvalidateUserChatsAsync(memberIds);
            await _cacheInvalidator.InvalidateChatMembersAsync(request.ChatId);
        }
    }
}