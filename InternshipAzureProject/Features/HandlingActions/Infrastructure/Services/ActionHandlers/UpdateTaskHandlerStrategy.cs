using Domain.Entities;
using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Interfaces;
using System.Text;

namespace Infrastructure.Services.ActionHandlers
{
    public class UpdateTaskHandlerStrategy : IActionHandlerStrategy
    {
        private readonly ITaskFacade _taskFacade;
        private readonly IBotService _botService;
        private static List<PendingAction> _pendingActions;

        public ActionType ActionType => ActionType.UpdateTaskByName;

        public UpdateTaskHandlerStrategy(ITaskFacade taskFacade, IBotService botService, List<PendingAction> pendingActions)
        {
            _taskFacade = taskFacade;
            _botService = botService;
            _pendingActions = pendingActions;
        }

        public async Task HandleAsync(ParsedMessageDto parsed, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            if (string.IsNullOrWhiteSpace(parsed.TaskName))
            {
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
                    $"❓ I didn't understand your message. A task name is required.");
                return;
            }

            var updateDto = new UpdateTaskDto { Title = parsed.NewName };

            if (parsed.PercentComplete.HasValue)
                updateDto.PercentComplete = parsed.PercentComplete.Value;

            if (parsed.EndDate.HasValue && parsed.StartDate.HasValue && parsed.EndDate < parsed.StartDate)
            {
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
                    $"❓ The due date cannot be earlier than the start date.");
                return;
            }

            if (parsed.StartDate.HasValue)
                updateDto.StartDate = parsed.StartDate.Value;

            if (parsed.EndDate.HasValue)
                updateDto.DueDate = parsed.EndDate.Value;

            var tasks = await _taskFacade.GetTasksByNameAsync(parsed.TaskName);
            if (tasks.Count == 0)
            {
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
                    $"⚠️ No task found with given name: '{parsed.TaskName}'.");
            }
            else if (tasks.Count == 1)
            {
                var targetUserIds = parsed.userIds ?? new List<string> { messageDto.FromUserId };
                targetUserIds = targetUserIds.Select(id => id == "-10" ? messageDto.FromUserId : id).ToList();
                updateDto.UsersToAssign = targetUserIds;
                var updated = await _taskFacade.UpdateTaskByIdAsync(tasks.First().Id, updateDto);
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
                    $"✏️ Task '{updated.Title}' was updated.");
            }
            else
            {
                // if several tasks found
                _pendingActions.Add(new PendingAction(
                    userId: messageDto.FromUserId,
                    tasks: tasks,
                    updatedTask: updateDto,
                    users: null,
                    TargetUserIds: parsed.userIds,
                    percentComplete: null,
                    time: null,
                    actionType: ActionType.UpdateTaskByName,
                    content: null
                ));

                var sb = new StringBuilder();
                sb.AppendLine($"🔎 Found several tasks with the name '{parsed.TaskName}'. Please specify which one to update:");
                for (int i = 0; i < tasks.Count; i++)
                    sb.AppendLine($"{i + 1}. {tasks[i].Title} (ID: {tasks[i].TaskId})");

                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, sb.ToString());
            }
        }
    }
}
