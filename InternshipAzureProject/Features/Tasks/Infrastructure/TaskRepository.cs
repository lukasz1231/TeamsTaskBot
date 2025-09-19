using Microsoft.EntityFrameworkCore;
using Domain.Entities.Models;
using Domain.Interfaces;
using Common.Data;

namespace Infrastructure.Services
{
    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;
        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<TaskModel>> GetTasksByNameAsync(string name)
        {
            if (name == null)
                throw new Exception($"Name is empty");
            var normalizedTitle = NormalizeAsci(name);

            var tasks = await _context.Tasks.Where(t => t.NormalizedTitle == normalizedTitle).ToListAsync();

            if (tasks == null)
                throw new Exception($"No task with name '{name}' found.");

            return tasks;
        }

        public async Task<TaskModel> GetTaskById(int id)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
            {
                throw new Exception($"Task with id '{id}' not found.");
            }
            return task;
        }
        public async Task<List<TaskModel>> GetAllAsync()
        {
            return await _context.Tasks
                .Include(t => t.TimeEntries)
                .Include(t => t.Comments)
                .ToListAsync();
        }
        public async Task CreateTaskAsync(TaskModel task)
        {
            if (task.DueDate.HasValue)
            {
                task.DueDate = task.DueDate.Value.ToUniversalTime();
            }
            if (task.StartDate.HasValue)
            {
                task.StartDate = task.StartDate.Value.ToUniversalTime();
            }
            await _context.Tasks.AddAsync(task);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateTaskAsync(TaskModel task)
        {
            if (task.DueDate.HasValue)
                task.DueDate = task.DueDate.Value.ToUniversalTime();

            if (task.StartDate.HasValue)
                task.StartDate = task.StartDate.Value.ToUniversalTime();

            var existingTask = await _context.Tasks
                .Include(t => t.Users) 
                .FirstOrDefaultAsync(t => t.Id == task.Id);

            if (existingTask == null)
                throw new InvalidOperationException($"Task {task.Id} not found");

            _context.Entry(existingTask).CurrentValues.SetValues(task);


            foreach (var user in task.Users)
            {
                if (!existingTask.Users.Any(u => u.UserId == user.UserId))
                {
                    _context.Users.Attach(user); // attach stub
                    existingTask.Users.Add(user);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteTaskAsync(TaskModel task)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }

        public async Task CreateCommentAsync(CommentModel comment)
        {
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetAssignedUsersToTask(int taskId)
        {
            var task = await _context.Tasks
                                     .Include(t => t.Users) // Eagerly load the related Users
                                     .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                // Handle case where task is not found
                throw new Exception($"Task with id '{taskId}' not found.");
            }

            return task.Users?.Select(u => u.UserId).ToList() ?? new List<string>();
        }
        public async Task<List<TaskModel>> GetUserTasksAsync(string userId)
        {
            var user = await _context.Users
                                     .Include(u => u.Tasks)
                                     .FirstOrDefaultAsync(u => u.UserId == userId);
            if(user == null)
            {
                throw new Exception($"User with id '{userId}' not found.");
            }
            // if the user is found, return their tasks, otherwise, return an empty list.
            return user.Tasks.ToList() ?? new List<TaskModel>();
        }
        private static string NormalizeAsci(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var normalized = name.ToLowerInvariant()
                                  .Replace(" ", "")
                                  .Replace("ą", "a")
                                  .Replace("ć", "c")
                                  .Replace("ę", "e")
                                  .Replace("ł", "l")
                                  .Replace("ń", "n")
                                  .Replace("ó", "o")
                                  .Replace("ś", "s")
                                  .Replace("ż", "z")
                                  .Replace("ź", "z");

            return normalized;
        }
    }

}

