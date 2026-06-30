using Microsoft.AspNetCore.SignalR;

using LightChat.Core.Entities;
using LightChat.Core.Repositories;

namespace LightChat.Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IChatRepository _chatRepository;

        public ChatHub(IMessageRepository messageRepository, IChatRepository chatRepository)
        {
            _messageRepository = messageRepository;
            _chatRepository = chatRepository;
        }

        /// <summary>
        /// Отправка сообщения в конкретный чат
        /// </summary>
        public async Task SendMessage(Guid chatId, Guid userId, string text)
        {
            var isMember = await _chatRepository.IsMemberAsync(chatId, userId);
            if (!isMember)
                throw new HubException("Вы не являетесь участником этого чата.");

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = userId,
                Text = text,
                SentAt = DateTime.UtcNow
            };

            await _messageRepository.SaveAsync(message);

            await Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                chatId = message.ChatId,
                senderId = message.SenderId,
                text = message.Text,
                sentAt = message.SentAt
            });
        }

        /// <summary>
        /// Присоединение юзера к чату
        /// </summary>
        public async Task JoinChat(Guid chatId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());

        /// <summary>
        /// Покидание юзером чата
        /// </summary>
        public async Task LeaveChat(Guid chatId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());
    }
}