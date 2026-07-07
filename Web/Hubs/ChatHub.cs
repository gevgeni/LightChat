using LightChat.Core.Entities;
using LightChat.Core.Repositories;
using LightChat.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LightChat.Web.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IChatRepository _chatRepository;
        private readonly IUserRepository _userRepository;

        private readonly IUserStatusManager _statusManager;

        public ChatHub(IMessageRepository messageRepository, IChatRepository chatRepository, IUserRepository userRepository, IUserStatusManager statusManager)
        {
            _messageRepository = messageRepository;
            _chatRepository = chatRepository;
            _userRepository = userRepository;

            _statusManager = statusManager;
        }

        public override async Task OnConnectedAsync()
        {
            var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out var userId))
            {
                _statusManager.SetOnline(userId, Context.ConnectionId);

                await Clients.All.SendAsync("UserStatusChanged", new { userId, isOnline = true });
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _statusManager.RemoveConnection(Context.ConnectionId, out var userId, out var isFullyOffline);

            if (isFullyOffline && userId.HasValue)
                await Clients.All.SendAsync("UserStatusChanged", new { userId, isOnline = false });

            await base.OnDisconnectedAsync(exception);
        }
        /// <summary>
        /// Уведомление о том, что пользователь печатает
        /// </summary>
        public async Task NotifyTyping(Guid chatId)
        {
            var userId = GetUserId();

            await Clients.GroupExcept(chatId.ToString(), Context.ConnectionId)
                .SendAsync("UserIsTyping", new { userId, chatId });
        }

        /// <summary>
        /// Отправка сообщения в конкретный чат
        /// </summary>
        public async Task SendMessage(Guid chatId, string text)
        {
            var userId = GetUserId();

            if (Context.Items["AuthorizedChats"] is not HashSet<Guid> authorizedChats 
                || !authorizedChats.Contains(chatId))
            {
                throw new HubException("Вы не являетесь участником этого чата.");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            var username = user?.Username ?? "Неизвестный";

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
                senderUsername = username,
                text = message.Text,
                sentAt = message.SentAt
            });
        }

        /// <summary>
        /// Присоединение юзера к чату
        /// </summary>
        public async Task JoinChat(Guid chatId)
        {
            var userId = GetUserId();

            var isMember = await _chatRepository.IsMemberAsync(chatId, userId);
            if (!isMember)
                throw new HubException("Вы не являетесь участником этого чата.");

            await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());

            if (Context.Items["AuthorizedChats"] is not HashSet<Guid> authorizedChats)
            {
                authorizedChats = [];
                Context.Items["AuthorizedChats"] = authorizedChats;
            }
            authorizedChats.Add(chatId);
        }

        /// <summary>
        /// Покидание юзером чата
        /// </summary>
        public async Task LeaveChat(Guid chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());

            if (Context.Items["AuthorizedChats"] is HashSet<Guid> authorizedChats)
                authorizedChats.Remove(chatId);
        }

        private Guid GetUserId()
        {
            var nameIdentifier = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
                throw new HubException("Не удалось определить идентификатор пользователя.");

            return userId;
        }
    }
}