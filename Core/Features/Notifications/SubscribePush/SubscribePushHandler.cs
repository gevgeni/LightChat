using LightChat.Core.Entities;
using LightChat.Core.Repositories;
using MediatR;

namespace LightChat.Core.Features.Notifications.SubscribePush
{
    public class SubscribePushHandler : IRequestHandler<SubscribePushCommand, SubscribePushDto>
    {
        private readonly IPushSubscriptionRepository _subscriptionRepository;

        public SubscribePushHandler(IPushSubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<SubscribePushDto> Handle(SubscribePushCommand request, CancellationToken cancellationToken)
        {
            var subscription = new UserPushSubscription
            {
                UserId = request.UserId,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth
            };
            await _subscriptionRepository.AddOrUpdateSubscriptionAsync(subscription);

            return new SubscribePushDto(subscription.UserId, subscription.Endpoint);
        }
    }
}
