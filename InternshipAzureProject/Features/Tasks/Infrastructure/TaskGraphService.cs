using Domain.Entities.Dtos;
using Common.Options;
using Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Infrastructure.Services
{

    public class TaskGraphService : ITaskGraphService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly IOptions<DefaultValuesOption> _configuration;

        public TaskGraphService(GraphServiceClient graphClient, IOptions<DefaultValuesOption> cfg)
        {
            _graphClient = graphClient;
            _configuration = cfg;
        }

        public async Task<PlannerTask> CreateTaskAsync(string Title, DateTimeOffset? StartDate = null, DateTimeOffset? DueDate = null)
        {
            var planId = _configuration.Value.planId;
            var bucketId = _configuration.Value.bucketId;
            var plannerTask = new PlannerTask
            {
                Title = Title,
                PlanId = planId,
                BucketId = _configuration.Value.bucketNotStartedId,
                StartDateTime = DateTimeOffset.UtcNow,
                PercentComplete = 0,
            };
            if (StartDate.HasValue)
            {
                plannerTask.StartDateTime = StartDate.Value;
            }
            if (DueDate.HasValue)
            {
                plannerTask.DueDateTime = DueDate;
            }
            return await _graphClient.Planner.Tasks.PostAsync(plannerTask);
        }

        // create a task for a specific user, that belongs to a default plan and bucket
        public async Task<PlannerTask> CreateUserTaskAsync(string userId, string Title, DateTimeOffset? StartDate = null, DateTimeOffset? DueDate = null)
        {
            var planId = _configuration.Value.planId;
            
            var plannerTask = new PlannerTask
            {
                Title = Title,
                PlanId = planId,
                BucketId = _configuration.Value.bucketNotStartedId,
                PercentComplete = 0,

                Assignments = new PlannerAssignments
                {
                    AdditionalData = new Dictionary<string, object>
        {
            {
                userId , new PlannerAssignment
                {
                    OdataType = "#microsoft.graph.plannerAssignment",
                    OrderHint = " !",
                }
            },
        },
                },
            };
            if (StartDate.HasValue)
            {
                plannerTask.StartDateTime = StartDate.Value;
            }
            if (DueDate.HasValue)
            {
                plannerTask.DueDateTime = DueDate;
            }

            return await _graphClient.Planner.Tasks.PostAsync(plannerTask);
        }
        public async Task AddUsersToTask(List<string> userIds, string taskId)
        {
            var task = await _graphClient.Planner.Tasks[taskId].GetAsync(requestConfig =>
            {
                requestConfig.Headers.Add("Prefer", "return=representation");
            });
            if (task == null)
            {
                throw new InvalidOperationException($"Task with ID {taskId} not found.");
            }
            // Retrieve the ETag from the task's AdditionalData dictionary
            if (!task.AdditionalData.TryGetValue("@odata.etag", out var etagObj) || etagObj is not string etag)
            {
                throw new InvalidOperationException($"ETag not found for task with ID {taskId}.");
            }
            var assignments = task.Assignments ?? new PlannerAssignments
            {
                AdditionalData = new Dictionary<string, object>()
            };
            foreach (var userId in userIds)
            {
                if (!assignments.AdditionalData.ContainsKey(userId))
                {
                    assignments.AdditionalData[userId] = new PlannerAssignment
                    {
                        OdataType = "#microsoft.graph.plannerAssignment",
                        OrderHint = " !"
                    };
                }
            }
            var updatedTask = new PlannerTask
            {
                Id = taskId,
                Title = task.Title, // keep original title (required for PATCH)
                Assignments = assignments
            };
            await _graphClient.Planner.Tasks[taskId].PatchAsync(updatedTask, requestConfig =>
            {
                requestConfig.Headers.Add("If-Match", etag);
            });
        }


        public async Task UpdateTaskAsync(string taskId, UpdateTaskDto updatedTask)
        {
            var task = await _graphClient.Planner.Tasks[taskId].GetAsync(requestConfig =>
            {
                requestConfig.Headers.Add("Prefer", "return=representation");
            });

            if (task == null)
            {
                throw new InvalidOperationException($"Task with ID {taskId} not found.");
            }

            // Retrieve the ETag from the task's AdditionalData dictionary
            if (!task.AdditionalData.TryGetValue("@odata.etag", out var etagObj) || etagObj is not string etag)
            {
                throw new InvalidOperationException($"ETag not found for task with ID {taskId}.");
            }

           
            var plannerTask = new PlannerTask
            {
                Id = taskId,
                Title = task.Title, // keep original title if not updated (required for PATCH)
                DueDateTime = updatedTask.DueDate,
                PercentComplete = updatedTask.PercentComplete,
            };
            // check if data is provided
            if (updatedTask.Title != null)
            {
                plannerTask.Title = updatedTask.Title;
            }

            if (updatedTask.DueDate.HasValue)
            {
                plannerTask.DueDateTime = updatedTask.DueDate;
            }

            if (updatedTask.PercentComplete.HasValue)
            {
                plannerTask.PercentComplete = updatedTask.PercentComplete;
                if (plannerTask.PercentComplete == 100)
                {
                    // if task is completed, move to completed bucket
                    plannerTask.BucketId = _configuration.Value.bucketFinishedId;
                }
                else if (plannerTask.PercentComplete > 0)
                {
                    // if task is in progress, move to in-progress bucket
                    plannerTask.BucketId = _configuration.Value.bucketStartedId;
                }
                else
                {
                    // if task is not started, move to not-started bucket
                    plannerTask.BucketId = _configuration.Value.bucketNotStartedId;
                }
            }
            if (updatedTask.UsersToAssign != null)
            {
                // new assignments
                var assignments = new PlannerAssignments
                {
                    AdditionalData = new Dictionary<string, object>()
                };

                // add already assigned users
                if (task.Assignments?.AdditionalData != null)
                {
                    foreach (var existing in task.Assignments.AdditionalData.Keys)
                    {
                        assignments.AdditionalData[existing] = new PlannerAssignment
                        {
                            OdataType = "#microsoft.graph.plannerAssignment",
                            OrderHint = " !"
                        };
                    }
                }

                // add new users
                foreach (var userId in updatedTask.UsersToAssign)
                {
                    if (!assignments.AdditionalData.ContainsKey(userId))
                    {
                        assignments.AdditionalData[userId] = new PlannerAssignment
                        {
                            OdataType = "#microsoft.graph.plannerAssignment",
                            OrderHint = " !"
                        };
                    }
                }

                plannerTask.Assignments = assignments;
            }

            await _graphClient.Planner.Tasks[taskId].PatchAsync(plannerTask, requestConfig =>
            {
                requestConfig.Headers.Add("If-Match", etag);
            });
        }

        public async Task DeleteTaskAsync(string taskId)
        {
            var task = await _graphClient.Planner.Tasks[taskId].GetAsync(requestConfig =>
            {
                requestConfig.Headers.Add("Prefer", "return=representation");
            });
            if (task == null)
            {
                throw new InvalidOperationException($"Task with ID {taskId} not found.");
            }

            // Retrieve the ETag from the task's AdditionalData dictionary
            if (!task.AdditionalData.TryGetValue("@odata.etag", out var etagObj) || etagObj is not string etag)
            {
                throw new InvalidOperationException($"ETag not found for task with ID {taskId}.");
            }

            await _graphClient.Planner.Tasks[taskId].DeleteAsync(requestConfig =>
            {
                requestConfig.Headers.Add("If-Match", etag);
            });
        }





        //get all tasks for a default plan
        public async Task<List<PlannerTask>> GetAllTasksAsync()
        {
            var planId = _configuration.Value.planId; //use default plan ID from configuration
            var plannerTasks = await _graphClient.Planner.Plans[planId].Tasks.GetAsync();
            if (plannerTasks?.Value == null)
            {
                return new List<PlannerTask>(); // empty list if no tasks found
            }
            return plannerTasks.Value;
        }
    }
}
