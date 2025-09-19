using Microsoft.Graph.Models;
using Domain.Entities.Dtos;

namespace Domain.Interfaces
{
    public interface ITaskGraphService
    {
        public Task<PlannerTask> CreateTaskAsync(string Title, DateTimeOffset? StartDate = null, DateTimeOffset? DueDate = null);
        public Task UpdateTaskAsync(string taskId, UpdateTaskDto updatedTask);
        public Task<PlannerTask> CreateUserTaskAsync(string userId, string Title, DateTimeOffset? StartDate = null, DateTimeOffset? DueDate = null);
        public Task DeleteTaskAsync(string taskId);
        public Task AddUsersToTask(List<string> userIds, string taskId);
        public Task<List<PlannerTask>> GetAllTasksAsync();
    }
}
