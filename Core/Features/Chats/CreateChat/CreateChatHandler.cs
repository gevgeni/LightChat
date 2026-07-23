using MediatR;

using LightChat.Core.Entities;
using LightChat.Core.Repositories;

namespace LightChat.Core.Features.Chats.CreateChat
{
    public class CreateChatHandler : IRequestHandler<CreateChatCommand, ChatResultDto>
    {
        private readonly IChatRepository _chatRepository;

        public CreateChatHandler(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }

        public async Task<ChatResultDto> Handle(CreateChatCommand request, CancellationToken cancellationToken)
        {
            var chat = new Chat
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                IsDirect = false,
                CreatedAt = DateTime.UtcNow
            };

            var member = new ChatMember
            {
                ChatId = chat.Id,
                UserId = request.CreatorUserId,
                JoinedAt = DateTime.UtcNow
            };

            await _chatRepository.CreateGroupChatAsync(chat, member);

            return new ChatResultDto(chat.Id, chat.Name, chat.CreatedAt, chat.IsDirect);
        }
    }
}