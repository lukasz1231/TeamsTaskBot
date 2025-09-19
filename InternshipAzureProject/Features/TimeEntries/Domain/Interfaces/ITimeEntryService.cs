using Domain.Entities.Models;

namespace Domain.Interfaces
{
    public interface ITimeEntryService
    {
        Task<TimeEntryModel> StartTaskById(string userId, int taskId);
        Task<TimeEntryModel> StartTaskAsync(string userId, TaskModel localTask);

        Task<TimeEntryModel> StopTaskById(string userId, int taskId, int? percentComplete = null);
        Task<TimeEntryModel> StopTaskAsync(string userId, TaskModel localTask, int? percentComplete = null);

        Task<TimeEntryModel> AddTimeToTaskById(string userId, int taskId, double? time = null, int? percentComplete = null, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null);
        Task<TimeEntryModel> AddTimeToTaskAsync(string userId, TaskModel localTask, double? time = null, int? percentComplete = null, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null);

        public Task<List<TimeEntryModel>> GetActiveTimeEntriesAsync(string userId, int taskId);
        public Task<List<TaskModel>> GetActiveTasksAsync(string userId);
    }
}
