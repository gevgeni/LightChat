using LightChat.Core.Entities;
using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;
using MediatR;

namespace LightChat.Core.Features.Chats.CreateChat
{
    public class CreateChatHandler : IRequestHandler<CreateChatCommand, ChatResultDto>
    {
        private readonly IChatRepository _chatRepository;
        private readonly ICacheInvalidator _cacheInvalidator;

        public CreateChatHandler(IChatRepository chatRepository, ICacheInvalidator cacheInvalidator)
        {
            _chatRepository = chatRepository;
            _cacheInvalidator = cacheInvalidator;
        }

        public async Task<ChatResultDto> Handle(CreateChatCommand request, CancellationToken cancellationToken)
        {
            var chat = new Chat
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                IsDirect = false,
                CreatedAt = DateTime.UtcNow,
                CreatorId = request.CreatorUserId
            };

            var member = new ChatMember
            {
                ChatId = chat.Id,
                UserId = request.CreatorUserId,
                JoinedAt = DateTime.UtcNow
            };

            await _chatRepository.CreateGroupChatAsync(chat, member);

            await _cacheInvalidator.InvalidateUserChatsAsync(request.CreatorUserId);

            return new ChatResultDto(chat.Id, chat.Name, chat.CreatedAt, chat.IsDirect);
        }
    }
}