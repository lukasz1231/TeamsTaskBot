namespace Domain.Interfaces
{
    public interface ISubscriptionService
    {
        public Task<Microsoft.Graph.Models.Subscription> CreateSubscriptionAsync(string teamId, string channelId);
        public Task DeleteSubscriptionAsync(string subscriptionId);
        public Task<Microsoft.Graph.Models.Subscription> RefreshSubscriptionAsync(string subscriptionId);
        public Task<List<Microsoft.Graph.Models.Subscription>> GetSubscriptionsAsync();
        public Task<int> GetExpirationTimeAsync();
    }
}
