namespace Domain.Interfaces
{
    public interface ITaskRepository
    {
        public Task<List<Domain.Entities.Models.TaskModel>> GetTasksByNameAsync(string name);
        public Task<Domain.Entities.Models.TaskModel> GetTaskById(int id);
        public Task<List<Domain.Entities.Models.TaskModel>> GetAllAsync();
        public Task<List<Domain.Entities.Models.TaskModel>> GetUserTasksAsync(string userId);
        public Task<List<string>> GetAssignedUsersToTask(int taskId);
        public Task CreateTaskAsync(Domain.Entities.Models.TaskModel task);
        public Task UpdateTaskAsync(Domain.Entities.Models.TaskModel task);
        public Task DeleteTaskAsync(Domain.Entities.Models.TaskModel task);
        public Task CreateCommentAsync(Domain.Entities.Models.CommentModel comment);
    }
}
