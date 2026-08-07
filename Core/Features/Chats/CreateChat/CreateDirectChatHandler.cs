using LightChat.Core.Entities;
using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;
using MediatR;

namespace LightChat.Core.Features.Chats.CreateChat
{
    public class CreateDirectChatHandler : IRequestHandler<CreateDirectChatCommand, ChatResultDto>
    {
        private readonly IChatRepository _chatRepository;
        private readonly ICacheInvalidator _cacheInvalidator;

        public CreateDirectChatHandler(IChatRepository chatRepository, ICacheInvalidator cacheInvalidator)
        {
            _chatRepository = chatRepository;
            _cacheInvalidator = cacheInvalidator;
        }

        public async Task<ChatResultDto> Handle(CreateDirectChatCommand request, CancellationToken cancellationToken)
        {
            var existingChat = await _chatRepository.GetDirectChatAsync(request.CreatorUserId, request.TargetUserId);
            if (existingChat != null)
                return new ChatResultDto(existingChat.Id, existingChat.Name, existingChat.CreatedAt, existingChat.IsDirect);

            var newChat = new Chat
            {
                Id = Guid.NewGuid(),
                Name = "DM",
                IsDirect = true,
                CreatedAt = DateTime.UtcNow
            };

            var currentUser = new ChatMember { ChatId = newChat.Id, UserId = request.CreatorUserId, JoinedAt = DateTime.UtcNow };
            var targetUser = new ChatMember { ChatId = newChat.Id, UserId = request.TargetUserId, JoinedAt = DateTime.UtcNow };

            await _chatRepository.CreateDirectChatAsync(newChat, currentUser, targetUser);

            await _cacheInvalidator.InvalidateUserChatsAsync(request.CreatorUserId, request.TargetUserId);

            return new ChatResultDto(newChat.Id, newChat.Name, newChat.CreatedAt, newChat.IsDirect);
        }
    }
}