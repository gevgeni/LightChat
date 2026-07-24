using MediatR;
using LightChat.Core.Interfaces;
using LightChat.Core.Repositories;

namespace LightChat.Core.Features.Chats.GetChatMembers
{
    public class GetChatMembersHandler : IRequestHandler<GetChatMembersQuery, IEnumerable<ChatMembersDto>>
    {
        private readonly IChatRepository _chatRepository;
        private readonly IUserStatusManager _statusManager;

        public GetChatMembersHandler(IChatRepository chatRepository, IUserStatusManager statusManager)
        {
            _chatRepository = chatRepository;
            _statusManager = statusManager;
        }

        public async Task<IEnumerable<ChatMembersDto>> Handle(GetChatMembersQuery request, CancellationToken cancellationToken)
        {
            var membersAsUsers = await _chatRepository.GetMembersAsync(request.ChatId);

            var results = membersAsUsers.Select(u => new ChatMembersDto
            (
                u.Id,
                u.Username,
                u.Email,
                _statusManager.IsUserOnline(u.Id)
            ));

            return results;
        }
    }
}