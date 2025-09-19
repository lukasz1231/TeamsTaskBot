using Common.Options;
using Domain.Entities.Dtos;
using Domain.Entities.Models;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Services
{
    public class TaskFacade : ITaskFacade
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskGraphService _taskService;
        private readonly IUserRepository _userRepository;
        private readonly IOptions<DefaultValuesOption> _options;

        public TaskFacade(
            ITaskRepository taskRepository,
            ITaskGraphService taskGraphService,
            IUserRepository userRepository,
            IOptions<DefaultValuesOption> options)
        {
            _taskRepository = taskRepository;
            _taskService = taskGraphService;
            _userRepository = userRepository;
            _options = options;
        }



        /// <summary>
        /// Creates both local and graph task, retrieves taskId from graph
        /// </summary>
        public async Task<TaskModel> CreateTaskAsync(string taskName, DateTimeOffset? startDate = null, DateTimeOffset? dueDate = null)
        {
            var plannerTask = await _taskService.CreateTaskAsync(taskName, StartDate: startDate, DueDate: dueDate);
            if (plannerTask == null)
                throw new Exception("Failed to create task in Microsoft Planner.");
            var task = new TaskModel
                {
                TaskId = plannerTask.Id,
                Title = taskName,
                PlanId = _options.Value.planId,
                BucketId = _options.Value.bucketId,
                PercentComplete = 0,
            };
            if (startDate.HasValue && dueDate.HasValue && startDate > dueDate)
                throw new InvalidOperationException("StartDate must be before DueDate");
            if (startDate.HasValue)
            {
                task.StartDate = startDate.Value.ToUniversalTime();
            }
            if (dueDate.HasValue)
            {
                task.DueDate = dueDate.Value.ToUniversalTime();
            }
            await _taskRepository.CreateTaskAsync(task);
            return task;
        }


        public async Task<List<TaskModel>?> GetTasksByNameAsync(string taskName)
        {
            return await _taskRepository.GetTasksByNameAsync(taskName);
        }


        /// <summary>
        /// Updates a task, by id in both the local repository and in Microsoft Graph.
        /// UpdateTaskDto allows update of only chosen fields of TaskModel.
        /// </summary>
        public async Task<TaskModel?> UpdateTaskByIdAsync(int taskId, UpdateTaskDto update)
        {
            var task = await _taskRepository.GetTaskById(taskId);
            if (task == null) return null;
            return await UpdateTaskAsync(task, update);
        }


        public async Task<TaskModel?> UpdateTaskAsync(TaskModel task, UpdateTaskDto update)
        {
            // common update logic
            if (!string.IsNullOrEmpty(update.Title))
                task.Title = update.Title;

            if (update.PercentComplete.HasValue)
                task.PercentComplete = update.PercentComplete.Value;

            DateTimeOffset? newStartDate = update.StartDate ?? task.StartDate;
            DateTimeOffset? newDueDate = update.DueDate ?? task.DueDate;

            if (newStartDate.HasValue && newDueDate.HasValue && newStartDate > newDueDate)
                throw new ArgumentException("Start date cannot be later than due date.");

            task.StartDate = newStartDate;
            task.DueDate = newDueDate;

            var existingUserIds = await _taskRepository.GetAssignedUsersToTask(task.Id);
            var usersToAssignIds = update.UsersToAssign ?? new List<string>();

            var userIdsToAdd = usersToAssignIds.Except(existingUserIds).ToList();
            if (update.UsersToAssign != null)
            {
                var users = new List<UserModel>();
                foreach (var userId in update.UsersToAssign)
                    
                    users.Add( await _userRepository.GetUserById(userId) );
                
                foreach (var user in users)
                {
                    task.Users.Add(user);
                }
            }
        
            
            // save in db
            await _taskRepository.UpdateTaskAsync(task);

            // save in Graph
            await _taskService.UpdateTaskAsync(task.TaskId, new Domain.Entities.Dtos.UpdateTaskDto
            {
                Title = update.Title ?? task.Title,
                PercentComplete = update.PercentComplete ?? (int)task.PercentComplete,
                StartDate = update.StartDate ?? task.StartDate,
                DueDate = update.DueDate ?? task.DueDate,
                UsersToAssign = usersToAssignIds
            });

            return task;
        }
        public async Task<bool> IsUserAssignedAsync(int taskId, string userId)
        {
            var assignedUserIds = await _taskRepository.GetAssignedUsersToTask(taskId);
            return assignedUserIds.Contains(userId);
        }


        /// <summary>
        /// Deletes a task from both the local repository and Microsoft Graph by Id.
        /// </summary>
        public async Task DeleteTaskByIdAsync(int taskId)
        {
            var task = await _taskRepository.GetTaskById(taskId);

            // delete locally
            await _taskRepository.DeleteTaskAsync(task);

            // delete in MS Graph
            await _taskService.DeleteTaskAsync(task.TaskId);
        }

        public async Task DeleteTaskAsync( TaskModel task)
        {
            // delete locally
            await _taskRepository.DeleteTaskAsync(task);
            // delete in MS Graph
            await _taskService.DeleteTaskAsync(task.TaskId);
        }

        public async Task AddCommentAsync(string userId, TaskModel task, string commentContent)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task), "Task cannot be null.");
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(commentContent))
                throw new ArgumentException("User ID and comment content cannot be null or empty.");

            var localUser = await _userRepository.GetUserById(userId);
            if (localUser == null)
                throw new InvalidOperationException("User not found in local database.");

            var newComment = new CommentModel
            {
                Content = commentContent,
                LocalTaskId = task.Id,
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _taskRepository.CreateCommentAsync(newComment);
        }


        public async Task AddCommentByTaskIdAsync(string userId, int taskId, string commentContent)
        {
            if (taskId<0)
                throw new ArgumentException("Invalid Task ID.");
            var localTask = await _taskRepository.GetTaskById(taskId);
            if (localTask == null)
                throw new InvalidOperationException("Task not found in local database.");
            await AddCommentAsync(userId, localTask, commentContent);
        }

    }
}
