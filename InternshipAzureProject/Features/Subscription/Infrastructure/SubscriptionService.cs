using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly Microsoft.Graph.GraphServiceClient _graphClient;
        public SubscriptionService(Microsoft.Graph.GraphServiceClient graphClient)
        {
            _graphClient = graphClient;
        }
        // subscription methods
        public async Task<Microsoft.Graph.Models.Subscription> CreateSubscriptionAsync(string teamId, string channelId)
        {
            var subscription = new Microsoft.Graph.Models.Subscription
            {
                ChangeType = "created",
                NotificationUrl = "https://gs2qbtgm-7222.euw.devtunnels.ms/api/webhook/get",
                Resource = $"/teams/{teamId}/channels/{channelId}/messages",
                ExpirationDateTime = DateTimeOffset.UtcNow.AddMinutes(55),
                ClientState = "Guid.NewGuid().ToString()"
            };

            return await _graphClient.Subscriptions.PostAsync(subscription);
        }

        public async Task DeleteSubscriptionAsync(string subscriptionId)
        {
            await _graphClient.Subscriptions[subscriptionId].DeleteAsync();
        }

        public async Task<Microsoft.Graph.Models.Subscription> RefreshSubscriptionAsync(string subscriptionId)
        {
            var subscription = new Microsoft.Graph.Models.Subscription
            {
                ExpirationDateTime = DateTimeOffset.UtcNow.AddMinutes(55)
            };

            return await _graphClient.Subscriptions[subscriptionId].PatchAsync(subscription);
        }
        public async Task<List<Microsoft.Graph.Models.Subscription>> GetSubscriptionsAsync()
        {
            var subscriptions = await _graphClient.Subscriptions.GetAsync();
            return subscriptions.Value.ToList();
        }
        public async Task<int> GetExpirationTimeAsync()
        {
            var subscriptions = await _graphClient.Subscriptions.GetAsync();
            if (subscriptions.Value == null || !subscriptions.Value.Any())
            {
                return 0; // No subscriptions found
            }
            var subscription = subscriptions.Value.FirstOrDefault();
            if (subscription == null || subscription.ExpirationDateTime == null)
            {
                return 0; // No valid subscription found
            }
            var expirationTime = subscription.ExpirationDateTime;
            var currentTime = DateTimeOffset.UtcNow;
            var timeDifference = expirationTime - currentTime;
            return (int)timeDifference?.TotalMinutes;
        }
    }
}
