using MediatR;

using LightChat.Core.Repositories;
using LightChat.Core.Features.Chats.CreateChat;

namespace LightChat.Core.Features.Chats.GetUserChats
{
    public class GetUserChatsHandler : IRequestHandler<GetUserChatsQuery, IEnumerable<ChatResultDto>>
    {
        private readonly IChatRepository _chatRepository;

        public GetUserChatsHandler(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }

        public async Task<IEnumerable<ChatResultDto>> Handle(GetUserChatsQuery request, CancellationToken cancellationToken)
        {
            var userChats = await _chatRepository.GetUserChatsAsync(request.UserId);
            
            var result = userChats.Select(c =>
            {
                string finalName = c.Name;

                if (c.IsDirect)
                {
                    var members = c.ChatMembers;

                    var companion = members.FirstOrDefault(m => m.UserId != request.UserId);
                    finalName = companion != null ? companion.User.Username : "Удаленный пользователь";
                }

                return new ChatResultDto
                (
                    c.Id,
                    finalName,
                    c.CreatedAt,
                    c.IsDirect
                );
            });

            return result;
        }
    }
}