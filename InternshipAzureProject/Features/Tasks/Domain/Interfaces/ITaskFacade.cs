using Domain.Entities.Dtos;
using Domain.Entities.Models;

namespace Domain.Interfaces
{
    public interface ITaskFacade
    {
        public Task<TaskModel> CreateTaskAsync(string taskName, DateTimeOffset? startDate = null, DateTimeOffset? dueDate = null);
        public Task<List<TaskModel>?> GetTasksByNameAsync(string taskName);
        public Task<TaskModel> UpdateTaskByIdAsync(int taskId, UpdateTaskDto update);
        public Task<TaskModel?> UpdateTaskAsync(TaskModel task, UpdateTaskDto update);
        public Task DeleteTaskByIdAsync(int taskId);
        public Task DeleteTaskAsync(TaskModel task);
        public Task AddCommentByTaskIdAsync(string userId, int taskId, string commentContent);
        public Task AddCommentAsync(string userId, TaskModel task, string commentContent);
        public Task<bool> IsUserAssignedAsync(int taskId, string userId);
    }
}
