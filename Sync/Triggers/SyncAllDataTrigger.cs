using Domain.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Functions.Triggers
{
    public class SyncAllDataTrigger
    {
        private readonly ILogger<SyncAllDataTrigger> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SyncAllDataTrigger(ILogger<SyncAllDataTrigger> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [Function("SyncAllDataTrigger")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo timer)
        {
            _logger.LogInformation("🚀 Starting data synchronization (users and tasks).");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

                _logger.LogInformation("⚙️ Starting user synchronization...");
                await syncService.SyncUsersAsync();
                _logger.LogInformation("✅ User synchronization completed successfully.");

                _logger.LogInformation("⚙️ Starting task synchronization...");
                await syncService.SyncTasksToPlannerAsync();
                _logger.LogInformation("✅ Task synchronization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ An error occurred during data synchronization.");
            }
        }
    }
}
