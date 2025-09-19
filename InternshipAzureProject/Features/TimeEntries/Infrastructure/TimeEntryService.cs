using Domain.Entities.Models;
using Domain.Entities.Dtos;
using Domain.Interfaces;
using Common.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class TimeEntryService : ITimeEntryService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISyncService _syncService;
        private readonly ITaskFacade _taskFacade;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<TimeEntryService> _logger;
        public TimeEntryService(
            ITaskRepository taskRepository,
            AppDbContext context,
            ISyncService sync,
            IUserRepository userRepository,
            ILogger<TimeEntryService> logger,
            ITaskFacade taskFacade)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _syncService = sync;
            _dbContext = context;
            _logger = logger;
            _taskFacade = taskFacade;
        }

        public async Task<TimeEntryModel> StartTaskById(string userId, int taskId)
        {
            var localTask = await _taskRepository.GetTaskById(taskId);
            if (localTask == null)
                throw new InvalidOperationException("Task not found in local database.");

            return await StartTaskAsync(userId, localTask);
        }

        public async Task<TimeEntryModel> StartTaskAsync(string userId, TaskModel localTask)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be null or empty.");

            if (localTask.PercentComplete == 100)
                throw new InvalidOperationException("Cannot start a task that is already 100% complete.");

            var localUser = await _userRepository.GetUserById(userId);
            if (localUser == null)
                throw new InvalidOperationException("User not found in local database.");
            var isAssigned = await _taskFacade.IsUserAssignedAsync(localTask.Id, userId);
            if (!isAssigned)
                throw new InvalidOperationException("User is not assigned to this task and cannot start it.");

            // Update task in planner
            var updatedTask = new UpdateTaskDto
            {
                PercentComplete = 50,
                UsersToAssign = new List<string> { userId },
                StartDate = localTask.StartDate == null || localTask.StartDate == DateTimeOffset.MinValue
                            ? DateTimeOffset.UtcNow
                            : localTask.StartDate?.ToUniversalTime()
            };

            await _taskFacade.UpdateTaskAsync(localTask, updatedTask);

            var newTimeEntry = new TimeEntryModel
            {
                UserId = userId,
                LocalTaskId = localTask.Id,
                StartTime = DateTimeOffset.UtcNow,
                EndTime = null
            };

            _dbContext.TimeEntries.Add(newTimeEntry);
            await _dbContext.SaveChangesAsync();

            return newTimeEntry;
        }

        public async Task<TimeEntryModel> StopTaskById(string userId, int taskId, int? percentComplete = null)
        {
            var localTask = await _taskRepository.GetTaskById(taskId);
            if (localTask == null)
                throw new InvalidOperationException("Task not found in local database.");

            return await StopTaskAsync(userId, localTask, percentComplete);
        }

        public async Task<TimeEntryModel> StopTaskAsync(string userId, TaskModel localTask, int? percentComplete = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be null or empty.");

            var timeEntry = await _dbContext.TimeEntries
                .Include(te => te.Task)
                .FirstOrDefaultAsync(te => te.UserId == userId
                                        && te.LocalTaskId == localTask.Id
                                        && te.EndTime == null);

            if (timeEntry == null)
                throw new InvalidOperationException("No active time entry found for the specified user and task.");

            timeEntry.EndTime = DateTimeOffset.UtcNow;
            timeEntry.Duration = (timeEntry.EndTime.HasValue && timeEntry.StartTime != null)
                ? (timeEntry.EndTime.Value - timeEntry.StartTime)
                : TimeSpan.Zero;

            if (percentComplete.HasValue)
            {
                await _taskFacade.UpdateTaskAsync(localTask, new UpdateTaskDto
                {
                    PercentComplete = percentComplete.Value
                });
            }

            await _dbContext.SaveChangesAsync();

            return timeEntry;
        }

        public async Task<TimeEntryModel> AddTimeToTaskById(string userId, int taskId, double? time = null, int? percentComplete = null, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
        {
            var localTask = await _taskRepository.GetTaskById(taskId);
            if (localTask == null)
                throw new InvalidOperationException("Task not found in local database.");

            return await AddTimeToTaskAsync(userId, localTask, time, percentComplete, startTime, endTime);
        }

        public async Task<TimeEntryModel> AddTimeToTaskAsync(string userId, TaskModel localTask, double? time = null, int? percentComplete = null, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be null.");

            if (localTask.PercentComplete == 100)
                throw new InvalidOperationException("Cannot add time to a task that is already 100% complete.");

            var localUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (localUser == null)
                throw new InvalidOperationException("User not found in local database.");

            // calculate effective Start and End
            var (effectiveStart, effectiveEnd) = CalculateEffectiveStartEnd(time, startTime, endTime);

            var duration = effectiveEnd - effectiveStart;
            if (duration.TotalMinutes <= 0)
                throw new ArgumentException("Duration must be greater than zero.");

            var timeEntry = new TimeEntryModel
            {
                UserId = userId,
                LocalTaskId = localTask.Id,
                StartTime = effectiveStart.ToUniversalTime(),
                EndTime = effectiveEnd.ToUniversalTime(),
                Duration = duration
            };

            percentComplete ??= 50;
            if (percentComplete < 0 || percentComplete > 100)
                throw new ArgumentException("Percent complete must be between 0 and 100.");

            var updatedTask = new UpdateTaskDto
            {
                PercentComplete = percentComplete.Value,
                UsersToAssign = new List<string> { userId },
                StartDate = localTask.StartDate == null || localTask.StartDate == DateTimeOffset.MinValue
                            ? effectiveStart
                            : localTask.StartDate
            };

            await _taskFacade.UpdateTaskAsync(localTask, updatedTask);

            _dbContext.TimeEntries.Add(timeEntry);
            await _dbContext.SaveChangesAsync();

            return timeEntry;
        }

        private static (DateTimeOffset Start, DateTimeOffset End) CalculateEffectiveStartEnd(double? time, DateTimeOffset? startTime, DateTimeOffset? endTime)
        {
            DateTimeOffset effectiveStart, effectiveEnd;

            if (startTime.HasValue && endTime.HasValue && time.HasValue)
            {
                effectiveStart = startTime.Value;
                effectiveEnd = startTime.Value.AddHours(time.Value);
            }
            else if (startTime.HasValue && endTime.HasValue)
            {
                effectiveStart = startTime.Value;
                effectiveEnd = endTime.Value;
                time ??= (endTime.Value - startTime.Value).TotalHours;
            }
            else if (startTime.HasValue && time.HasValue)
            {
                effectiveStart = startTime.Value;
                effectiveEnd = effectiveStart.AddHours(time.Value);
            }
            else if (endTime.HasValue && time.HasValue)
            {
                effectiveEnd = endTime.Value;
                effectiveStart = effectiveEnd.AddHours(-time.Value);
            }
            else if (time.HasValue)
            {
                effectiveEnd = DateTimeOffset.UtcNow;
                effectiveStart = effectiveEnd.AddHours(-time.Value);
            }
            else
            {
                throw new ArgumentException("Either time, or startTime with endTime must be provided.");
            }

            return (effectiveStart, effectiveEnd);
        }

        public async Task<List<TimeEntryModel>> GetActiveTimeEntriesAsync(string userId, int taskId)
        {
            return await _dbContext.TimeEntries
                .Where(te => te.UserId == userId &&
                             te.LocalTaskId == taskId &&
                             te.StartTime != null &&
                             (te.EndTime == null || te.EndTime == DateTimeOffset.MinValue))
                .ToListAsync();
        }

        public async Task<List<TaskModel>> GetActiveTasksAsync(string userId)
        {
            var activeTaskIds = await _dbContext.TimeEntries
                .Where(te => te.UserId == userId &&
                             te.StartTime != null &&
                             (te.EndTime == null || te.EndTime == DateTimeOffset.MinValue))
                .Select(te => te.LocalTaskId)
                .Distinct()
                .ToListAsync();

            var activeTasks = await _dbContext.Tasks
                .Where(t => activeTaskIds.Contains(t.Id))
                .ToListAsync();

            return activeTasks;
        }

    }
}
