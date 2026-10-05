using WebPush;
using MassTransit;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using LightChat.Core.Events;
using LightChat.Core.Repositories;

namespace LightChat.Infrastructure.Consumers
{
    public class MessageSentConsumer : IConsumer<MessageSentEvent>
    {
        private readonly WebPushClient _webPushClient;
        private readonly IConnectionMultiplexer _redis;
        private readonly IPushSubscriptionRepository _pushRepo;
        private readonly IConfiguration _config;
        private readonly ILogger<MessageSentConsumer> _logger;

        public MessageSentConsumer(
            WebPushClient webPushClient,
            IConnectionMultiplexer redis,
            IPushSubscriptionRepository pushRepo,
            IConfiguration config,
            ILogger<MessageSentConsumer> logger)
        {
            _webPushClient = webPushClient;
            _redis = redis;
            _pushRepo = pushRepo;
            _config = config;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<MessageSentEvent> context)
        {
            var message = context.Message;

            var vapidSubject = _config["Vapid:Subject"] ?? "";
            var publicKey = _config["Vapid:PublicKey"];
            var privateKey = _config["Vapid:PrivateKey"];

            if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
            {
                _logger.LogError("VAPID ключи не настроены в appsettings.json. Отмена отправки Push.");
                return;
            }

            var vapidDetails = new VapidDetails(vapidSubject, publicKey, privateKey);

            var db = _redis.GetDatabase();

            var recipientIds = await _pushRepo.GetChatRecipientIdsAsync(message.ChatId, message.SenderId);

            _logger.LogInformation(
                "Обработка MessageSentEvent: ChatId={ChatId}, Sender={Sender}, Recipients={Count}",
                message.ChatId, message.SenderId, recipientIds.Count());

            foreach (var recipientId in recipientIds)
            {
                var connectionsCount = await db.SetLengthAsync($"chat:user:{recipientId}:connections");

                if (connectionsCount > 0)
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
                        await _webPushClient.SendNotificationAsync(pushSubscription, payLoad, vapidDetails);

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