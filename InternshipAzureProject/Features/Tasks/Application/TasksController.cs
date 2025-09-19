using Common.Services;
using Domain.Entities.Dtos;
using Domain.Entities.Models;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{

    [Route("api/tasks")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IGraphMetadataService _graphMetadataService;
        private readonly ITaskFacade _taskFacade;
        private readonly ITimeEntryService _timeEntryService;
        public TasksController(ITaskRepository taskRepository, IGraphMetadataService graphMetadataService, ITaskFacade taskFacade, ITimeEntryService timeEntryService)
        {
            _taskRepository = taskRepository;
            _graphMetadataService = graphMetadataService;
            _taskFacade = taskFacade;
            _timeEntryService = timeEntryService;
        }

        /// <summary>
        /// Retrieves a list of tasks, with an optional filter to search by name.
        /// </summary>
        /// <param name="name">The optional name of the task to filter by.</param>
        /// <returns>An ActionResult containing a list of TaskModel objects. Returns 200 OK with the list of tasks.</returns>
        // GET /tasks?name=task1
        [HttpGet]
        public async Task<IActionResult> GetTasks([FromQuery] string? name)
        {
            var tasks = new List<TaskModel>();
            if (string.IsNullOrEmpty(name))
            {
                tasks = await _taskRepository.GetAllAsync();
            }
            else
            {
                tasks = await _taskRepository.GetTasksByNameAsync(name);
            }
            var showTasks = tasks.Select(t => new
            {
                t.Id,
                t.Title,
                t.StartDate,
                t.DueDate,
                t.PercentComplete
            });
            return Ok(showTasks);
        }
        /// <summary>
        /// Creates a new user task based on the provided data.
        /// </summary>
        /// <param name="task">The CreateTaskDto object containing the task's details, such as the title and due date.</param>
        /// <returns>An ActionResult representing the result of the creation operation. Returns 201 Created on success, or 400 Bad Request if the task data is invalid.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin,Task.Creator")]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto task)
        {
            if (task == null || string.IsNullOrEmpty(task.Title))
            {
                return BadRequest("Invalid task data.");
            }
            var createdTask = await _taskFacade.CreateTaskAsync( task.Title, task.DueDate ?? null);
            return Created();
        }

        /// <summary>
        /// Updates an existing task with new details such as title, start and due dates, percent completion, and user assignments.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        [HttpPatch("{taskId}")]
        [Authorize(Roles = "Admin,Task.Editor")]
        public async Task<IActionResult> UpdateUserTask(int taskId, [FromBody] UpdateTaskDto task)
        {
            if (task == null)
            {
                return BadRequest("Invalid task data.");
            }
            await _taskFacade.UpdateTaskByIdAsync(taskId, task);

            return NoContent();
        }
        /// <summary>
        /// Deletes a user task by its ID.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task to be deleted.</param>
        /// <returns>An ActionResult representing the result of the deletion operation. Returns 204 No Content on success.</returns>
        [HttpDelete("{taskId}")]
        [Authorize(Roles = "Admin,Task.Deleter")]
        public async Task<IActionResult> DeleteUserTask(int taskId)
        {
            await _taskFacade.DeleteTaskByIdAsync(taskId);
            return NoContent();
        }
        /// <summary>
        /// Adds a comment to the specified task.
        /// </summary>
        /// <remarks>This method requires the caller to be authorized with the "Admin" or "Task.Commenter"
        /// role. The user ID is extracted from the current user's claims and associated with the comment.</remarks>
        /// <param name="taskId">The unique identifier of the task to which the comment will be added.</param>
        /// <param name="comment">The content of the comment to add. Must not be null or empty.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.  Returns <see
        /// cref="BadRequestResult"/> if the <paramref name="comment"/> is null or empty.  Returns <see
        /// cref="CreatedResult"/> if the comment is successfully added.</returns>
        [HttpPost("{taskId}/comments")]
        [Authorize(Roles = "Admin, Task.Commenter")]
        public async Task<IActionResult> AddCommentToTask(int taskId, [FromBody] string comment)
        {
            if (comment == null || string.IsNullOrEmpty(comment))
            {
                return BadRequest("Invalid comment data.");
            }
            var userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            await _taskFacade.AddCommentByTaskIdAsync(userId, taskId, comment);
            return Created();
        }




    }
}
