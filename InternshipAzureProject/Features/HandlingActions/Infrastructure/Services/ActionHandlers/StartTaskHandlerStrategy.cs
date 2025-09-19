using Domain.Entities;
using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Interfaces;
using System.Text;

namespace Infrastructure.Services.ActionHandlers
{
    public class StartTaskHandlerStrategy : IActionHandlerStrategy
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly ITaskFacade _taskFacade;
        private readonly IBotService _botService;
        private static List<PendingAction> _pendingActions;

        public ActionType ActionType => ActionType.StartTaskByName;

        public StartTaskHandlerStrategy(ITimeEntryService timeEntryService, ITaskFacade taskFacade, IBotService botService, List<PendingAction> pendingActions)
        {
            _timeEntryService = timeEntryService;
            _taskFacade = taskFacade;
            _botService = botService;
            _pendingActions = pendingActions;
        }

        public async Task HandleAsync(ParsedMessageDto parsed, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            if (!string.IsNullOrWhiteSpace(parsed.TaskName))
            {
                var tasks = await _taskFacade.GetTasksByNameAsync(parsed.TaskName);
                if (tasks.Count == 0)
                {
                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, $"⚠️ No task found with given name: '{parsed.TaskName}'.");
                }
                else if (tasks.Count == 1)
                {
                    try
                    {
                        await _timeEntryService.StartTaskAsync(messageDto.FromUserId, tasks[0]);
                        await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, $"▶️ Started task '{tasks[0].Title}'.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, $"⚠️ Cannot start task: {ex.Message}");
                    }
                }
                else
                {
                    _pendingActions.Add(new PendingAction(messageDto.FromUserId, tasks, null, null, null, null, ActionType.StartTaskByName, null, null));

                    var sb = new StringBuilder();
                    sb.AppendLine($"🔎 Found several tasks with the name '{parsed.TaskName}'. Please specify which one to start:");
                    for (int i = 0; i < tasks.Count; i++)
                        sb.AppendLine($"{i + 1}. {tasks[i].Title} (ID: {tasks[i].TaskId})");

                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, sb.ToString());
                }
            }
            else
            {
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, $"❓ I didn't understand your message. A task name is required.");
            }
        }
    }
}