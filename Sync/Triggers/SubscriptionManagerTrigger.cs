using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Domain.Interfaces;
using Common.Options;
using Microsoft.Extensions.Options;

namespace Functions.Triggers
{
    public class SubscriptionManagerTrigger
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionManagerTrigger> _logger;
        private readonly IOptions<DefaultValuesOption> _options;

        public SubscriptionManagerTrigger(
            ISubscriptionService subscriptionService,
            ILogger<SubscriptionManagerTrigger> logger,
            IOptions<DefaultValuesOption> options)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
            _options = options;
        }

        [Function("SubscriptionManagerTrigger")]
        public async Task Run([TimerTrigger("0 */50 * * * *", RunOnStartup = true)] TimerInfo timer)
        {
            _logger.LogInformation("🚀 Starting subscription management process.");

            try
            {
                // Check if a subscription exists
                var subscriptions = await _subscriptionService.GetSubscriptionsAsync();
                var subscription = subscriptions.FirstOrDefault();

                if (subscription == null)
                {
                    _logger.LogInformation("No subscription found. Creating a new one...");

                    // Getting data from configuration is a recommended practice
                    string teamId = _options.Value.teamId;
                    string channelId = _options.Value.channelId;

                    subscription = await _subscriptionService.CreateSubscriptionAsync(teamId, channelId);
                    _logger.LogInformation($"New subscription created: {subscription.Id}");
                }
                else
                {
                    _logger.LogInformation($"Found an existing subscription: {subscription.Id}");
                }

                // Renew the subscription if its expiration is approaching
                var expiration = subscription?.ExpirationDateTime;
                if (expiration.HasValue && expiration.Value - DateTime.UtcNow < TimeSpan.FromMinutes(50))
                {
                    _logger.LogInformation($"🔄 Renewing subscription {subscription.Id} at {DateTime.UtcNow}");
                    await _subscriptionService.RefreshSubscriptionAsync(subscription.Id);
                    _logger.LogInformation("✅ Subscription renewed successfully.");
                }
                else if (expiration.HasValue)
                {
                    _logger.LogInformation($"Subscription {subscription.Id} is valid until {expiration.Value}. Renewal is not yet needed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ An error occurred during subscription management.");
            }
        }
    }
}