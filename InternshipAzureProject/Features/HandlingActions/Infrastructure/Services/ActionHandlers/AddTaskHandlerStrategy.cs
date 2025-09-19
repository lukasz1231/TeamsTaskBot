using Domain.Interfaces;
using Domain.Entities.Dtos;
using Domain.Entities.Enums;

namespace Infrastructure.Services.ActionHandlers
{
    public class AddTaskHandlerStrategy : IActionHandlerStrategy
    {
        private readonly ILogger<ActionHandler> _logger;
        private readonly ITaskFacade _taskFacade;
        private readonly IBotService _botService;

        public ActionType ActionType => ActionType.AddTask;

        public AddTaskHandlerStrategy(ILogger<ActionHandler> logger, ITaskFacade taskFacade, IBotService botService)
        {
            _logger = logger;
            _taskFacade = taskFacade;
            _botService = botService;
        }

        public async Task HandleAsync(ParsedMessageDto parsed, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            if (string.IsNullOrWhiteSpace(parsed.TaskName))
            {
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
                    $"❓ I didn't understand your message. A task name is required.");
                return;
            }

            // if specified - users, else message sender
            var targetUserIds = parsed.userIds ?? new List<string> { messageDto.FromUserId };
            targetUserIds = targetUserIds.Select(id => id == "-10" ? messageDto.FromUserId : id).ToList();


            _logger.LogInformation("Creating task with title: {TaskTitle}", parsed.TaskName);
            var newTask = await _taskFacade.CreateTaskAsync(parsed.TaskName);
            await _taskFacade.UpdateTaskAsync(newTask, new UpdateTaskDto { UsersToAssign =  targetUserIds });
            await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
            $"✅ Task '{newTask.Title}' was added.");
        }
    }
}
