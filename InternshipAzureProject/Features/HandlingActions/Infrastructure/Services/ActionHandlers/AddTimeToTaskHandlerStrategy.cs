using Domain.Entities;
using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Interfaces;
using System.Text;

namespace Infrastructure.Services.ActionHandlers
{
    public class AddTimeToTaskHandlerStrategy : IActionHandlerStrategy
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly ITaskFacade _taskFacade;
        private readonly IBotService _botService;
        private static List<PendingAction> _pendingActions;

        public ActionType ActionType => ActionType.AddTimeToTaskByName;

        public AddTimeToTaskHandlerStrategy(
            ITimeEntryService timeEntryService,
            ITaskFacade taskFacade,
            IBotService botService,
            List<PendingAction> pendingActions)
        {
            _timeEntryService = timeEntryService;
            _taskFacade = taskFacade;
            _botService = botService;
            _pendingActions = pendingActions;
        }

        public async Task HandleAsync(ParsedMessageDto parsed, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            if (!string.IsNullOrWhiteSpace(parsed.TaskName) && parsed.Time.HasValue)
            {
                var tasks = await _taskFacade.GetTasksByNameAsync(parsed.TaskName);
                if (tasks.Count == 0)
                {
                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
                        $"⚠️ No task found with given name: '{parsed.TaskName}'.");
                }
                else if (tasks.Count == 1)
                {
                    int percent = parsed.PercentComplete ?? 0;
                    var targetUserIds = parsed.userIds ?? new List<string> { messageDto.FromUserId };
                    targetUserIds = targetUserIds.Select(id => id == "-10" ? messageDto.FromUserId : id).ToList();

                    foreach (var userId in targetUserIds)
                    {
                        await _timeEntryService.AddTimeToTaskAsync(userId, tasks[0], parsed.Time.Value, percent);
                    }

                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
                        $"⏱️ Logged {parsed.Time.Value}h to task '{tasks[0].Title}' for {targetUserIds.Count} user(s).");
                }
                else
                {
                    // if several found
                    _pendingActions.Add(new PendingAction(
                        userId: messageDto.FromUserId,
                        tasks: tasks,
                        users: null,
                        TargetUserIds: parsed.userIds,
                        percentComplete: parsed.PercentComplete,
                        time: parsed.Time.Value,
                        actionType: ActionType.AddTimeToTaskByName,
                        updatedTask: null,
                        content: null
                    ));

                    var sb = new StringBuilder();
                    sb.AppendLine($"🔎 Found several tasks with the name '{parsed.TaskName}'. Please specify which one(s) to add time to:");
                    for (int i = 0; i < tasks.Count; i++)
                        sb.AppendLine($"{i + 1}. {tasks[i].Title} (ID: {tasks[i].TaskId})");

                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, sb.ToString());
                }
            }
            else
            {
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
                    $"❓ I didn't understand your message. A task name and time are required.");
            }
        }
    }
}
