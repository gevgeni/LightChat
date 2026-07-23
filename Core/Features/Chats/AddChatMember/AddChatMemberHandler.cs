using MediatR;

using LightChat.Core.Entities;
using LightChat.Core.Repositories;

namespace LightChat.Core.Features.Chats.AddChatMember
{
    public class AddChatMemberHandler : IRequestHandler<AddChatMemberCommand, AddMemberDto>
    {
        private readonly IChatRepository _chatRepository;
        public AddChatMemberHandler(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }

        public async Task<AddMemberDto> Handle(AddChatMemberCommand request, CancellationToken cancellationToken)
        {
            var chat = await _chatRepository.GetByIdAsync(request.ChatId) 
                ?? throw new KeyNotFoundException("Чат не найден.");
            if (chat.IsDirect)
                throw new InvalidOperationException("В личный чат нельзя приглашать сторонних участников.");

            var isCurrentMember = await _chatRepository.IsMemberAsync(request.ChatId, request.CurrentUserId);
            if (!isCurrentMember)
                throw new UnauthorizedAccessException("У вас нет доступа для добавления участников в этот чат.");

            var isAlreadyMember = await _chatRepository.IsMemberAsync(request.ChatId, request.TargetUserId);
            if (isAlreadyMember)
                throw new InvalidOperationException("Пользователь уже состоит в этом чате.");

            var member = new ChatMember
            {
                ChatId = request.ChatId,
                UserId = request.TargetUserId,
                JoinedAt = DateTime.UtcNow
            };

            await _chatRepository.AddMemberAsync(member);

            return new AddMemberDto(chat.Id, chat.Name, chat.IsDirect, request.TargetUserId);
        }
    }
}
