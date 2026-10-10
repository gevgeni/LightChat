using WebPush;
using MassTransit;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using LightChat.Core.Events;
using LightChat.Core.Repositories;
using LightChat.Core.Interfaces;

namespace LightChat.Infrastructure.Consumers
{
    public class MessageSentConsumer : IConsumer<MessageSentEvent>
    {
        private readonly WebPushClient _webPushClient;
        private readonly IPushSubscriptionRepository _pushRepo;
        private readonly IUserStatusManager _statusManager;
        private readonly VapidDetails _vapidDetails;
        private readonly ILogger<MessageSentConsumer> _logger;

        public MessageSentConsumer(
            WebPushClient webPushClient,
            IPushSubscriptionRepository pushRepo,
            IUserStatusManager statusManager,
            IConfiguration config,
            ILogger<MessageSentConsumer> logger)
        {
            _webPushClient = webPushClient;
            _pushRepo = pushRepo;
            _statusManager = statusManager;
            _logger = logger;

            var vapidSubject = config["Vapid:Subject"] ?? "";
            var publicKey = config["Vapid:PublicKey"];
            var privateKey = config["Vapid:PrivateKey"];

            if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
                throw new InvalidOperationException(
                    "VAPID ключи не настроены в appsettings.json");

            _vapidDetails = new VapidDetails(vapidSubject, publicKey, privateKey);
        }

        public async Task Consume(ConsumeContext<MessageSentEvent> context)
        {
            var message = context.Message;

            var recipientIds = await _pushRepo.GetChatRecipientIdsAsync(message.ChatId, message.SenderId);

            _logger.LogInformation(
                "Обработка MessageSentEvent: ChatId={ChatId}, Sender={Sender}, Recipients={Count}",
                message.ChatId, message.SenderId, recipientIds.Count());

            foreach (var recipientId in recipientIds)
            {
                if (await _statusManager.IsUserOnlineAsync(recipientId))
                {
                    _logger.LogInformation("Пользователь {UserId} в сети. Push не требуется.", recipientId);
                    continue;
                }

                var subscriptions = await _pushRepo.GetSubscriptionsByUserIdAsync(recipientId);
                if (!subscriptions.Any()) continue;

                var payLoad = JsonSerializer.Serialize(new
                {
                    title = $"Новое сообщение от {message.SenderUsername}",
                    body = message.Text,
                    chatId = message.ChatId
                });

                foreach (var sub in subscriptions)
                {
                    try
                    {
                        var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                        await _webPushClient.SendNotificationAsync(pushSubscription, payLoad, _vapidDetails);

                        _logger.LogInformation("Push-уведомление успешно отправлено пользователю {UserId}", recipientId);
                    }
                    catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                                                      ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("Подписка {SubId} больше недействительна ({Status}). Удаляем...", sub.Id, ex.StatusCode);
                        await _pushRepo.RemoveSubscriptionAsync(sub.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка отправки Push для {UserId}", recipientId);
                    }
                }
            }
        }
    }
}