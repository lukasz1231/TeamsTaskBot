using Common.Data;
using Common.Options;
using Domain.Entities.Dtos;
using Domain.Entities.Models;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
namespace Infrastructure.Services
{
    public class SyncService : ISyncService
    {
        private readonly AppDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly IUserGraphService _userGraphService;
        private readonly ITaskGraphService _taskGraphService;
        private readonly ITaskRepository _taskRepository;
        private readonly IOptions<DefaultValuesOption> _configuration;

        public SyncService(AppDbContext dbContext, IUserRepository userRepository, ITaskGraphService taskGraphService, IOptions<DefaultValuesOption> configuration, ITaskRepository taskRepository, IUserGraphService userGraphService)
        {
            _context = dbContext;
            _userRepository = userRepository;
            _taskGraphService = taskGraphService;
            _configuration = configuration;
            _taskRepository = taskRepository;
            _userGraphService = userGraphService;
        }

        public async Task SyncUsersAsync()
        {
            // Get all users from Microsoft Graph
            var graphUsers = await _userGraphService.GetAllAsync();

            // Add or update users in local DB
            foreach (var user in graphUsers)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == user.UserId);

                if (existingUser == null)
                {
                    _context.Users.Add(user);
                }
                else
                {
                    existingUser.DisplayName = user.DisplayName;
                    existingUser.Email = user.Email;
                }
            }

            // Remove users that are no longer in Graph
            var localUsers = await _context.Users.ToListAsync();
            var usersToRemove = localUsers
                .Where(u => !graphUsers.Any(g => g.UserId == u.UserId))
                .ToList();

            _context.Users.RemoveRange(usersToRemove);

            await _context.SaveChangesAsync();
        }

        public async Task SyncTasksToPlannerAsync()
        {
            // Retrieve all tasks from the local database
            var localTasks = await _taskRepository.GetAllAsync();

            // Retrieve all tasks from Teams Planner
            var plannerTaskDtos = await _taskGraphService.GetAllTasksAsync();

            // Map Planner graph model to local models 
            var plannerTasks = plannerTaskDtos.Select(plnr => new TaskModel
            {
                TaskId = plnr.Id,
                Title = plnr.Title,
                StartDate = plnr.StartDateTime,
                DueDate = plnr.DueDateTime,
                PercentComplete = (int)plnr.PercentComplete,
                PlanId = plnr.PlanId,
                BucketId = plnr.BucketId,
            }).ToList();


            // 1. Tasks to add (exist in local DB but not in Planner)
            var tasksToAdd = localTasks
                .Where(lt => !plannerTasks.Any(pt => pt.TaskId == lt.TaskId))
                .ToList();

            // 2. Tasks to update (exist in both, but are different)
            var tasksToUpdate = localTasks
                .Where(lt => plannerTasks.Any(pt =>
                    pt.TaskId == lt.TaskId && (
                    pt.Title != lt.Title ||
                    pt.StartDate != lt.StartDate ||
                    pt.DueDate != lt.DueDate ||
                    pt.PercentComplete != lt.PercentComplete
                    )))
                .ToList();

            // 3. Tasks to delete (exist in Planner but not in local DB)
            var tasksToDelete = plannerTasks
                .Where(pt => !localTasks.Any(lt => lt.TaskId == pt.TaskId))
                .ToList();


            // Add new tasks to Planner
            foreach (var task in tasksToAdd)
            {

                var createdTask = await _taskGraphService.CreateTaskAsync(task.Title, DueDate: task.DueDate);
                // Update the local task with the new Planner ID
                task.TaskId = createdTask.Id;
            }

            // Update existing tasks in Planner
            foreach (var task in tasksToUpdate)
            {
                var updatedPlannerTask = new UpdateTaskDto
                {
                    Title = task.Title,
                    StartDate = task.StartDate,
                    DueDate = task.DueDate,
                    PercentComplete = task.PercentComplete
                };
                await _taskGraphService.UpdateTaskAsync(task.TaskId,updatedPlannerTask);
            }

            // Delete tasks from Planner
            foreach (var task in tasksToDelete)
            {
                await _taskGraphService.DeleteTaskAsync(task.TaskId);
            }

        }


    }
}
