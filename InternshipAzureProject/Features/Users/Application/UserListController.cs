using Common.Services;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserListController : ControllerBase
    {
        private readonly IGraphMetadataService _graphMetadataService;
        private readonly IUserRepository _userRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskFacade _taskFacade;

        public UserListController(IGraphMetadataService graphMetadataService, IUserRepository userRepository, ITaskRepository taskRepository, ITaskFacade taskFacade)
        {
            _graphMetadataService = graphMetadataService;
            _userRepository = userRepository;
            _taskRepository = taskRepository;
            _taskFacade = taskFacade;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var users = await _userRepository.GetAllAsync();
            return Ok(users);
        }
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(string userId)
        {
            var user = await _graphMetadataService.GetUserMetadataAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }
        [HttpGet("{userId}/tasks")]
        public async Task<IActionResult> GetUserTasks(string userId)
        {
            var tasks = await _taskRepository.GetUserTasksAsync(userId);
            if (tasks == null || !tasks.Any())
            {
                return NotFound();
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
    }
}
