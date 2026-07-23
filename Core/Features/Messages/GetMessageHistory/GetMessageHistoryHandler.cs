using MediatR;

using LightChat.Core.Entities;
using LightChat.Core.Repositories;

namespace LightChat.Core.Features.Messages.GetMessageHistory
{
    public class GetMessageHistoryHandler : IRequestHandler<GetMessageHistoryQuery, IEnumerable<MessageDto>>
    {
        private readonly IChatRepository _chatRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        public GetMessageHistoryHandler(
            IChatRepository chatRepository, 
            IMessageRepository messageRepository, 
            IUserRepository userRepository)
        {
            _chatRepository = chatRepository;
            _messageRepository = messageRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<MessageDto>> Handle(GetMessageHistoryQuery request, CancellationToken cancellationToken)
        {
            var isMember = await _chatRepository.IsMemberAsync(request.ChatId, request.UserId);
            if (!isMember)
                throw new UnauthorizedAccessException("Вы не состоите в этом чате.");

            var effectiveLimit = request.Limit > 0 ? request.Limit : 50;
            var messages = await _messageRepository.GetChatHistoryAsync(request.ChatId, effectiveLimit, request.BeforeMessageId);

            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
            List<User> explicitUsers = await _userRepository.GetAllContainsInIdsAsync(senderIds);

            var usernamesDict = explicitUsers.ToDictionary(u => u.Id, u => u.Username);

            var result = messages.Select(m => new MessageDto
            (
                m.Id,
                m.ChatId,
                m.SenderId,
                usernamesDict.TryGetValue(m.SenderId, out var name) ? name : "Неизвестный",
                m.Text,
                m.SentAt,
                m.IsRead
            ));

            return result;
        }
    }
}