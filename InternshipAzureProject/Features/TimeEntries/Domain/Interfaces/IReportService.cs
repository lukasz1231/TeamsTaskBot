using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IReportService
    {
        public Task<UserReport> GetUserReportAsync(string userId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        public Task<OverallReport> GetOverallReportAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        
        public Task<TaskReport> GetTaskReportAsync(int taskId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
        public string GenerateOverallReportMessage(OverallReport report);
        public string GenerateTaskReportMessage(TaskReport report);
        public string GenerateUserReportMessage(UserReport report);

    }
}
