using Domain.Entities;
using Domain.Entities.Models;
using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Interfaces;
using System.Text;

namespace Infrastructure.Services.ActionHandlers
{
    public class StopTaskHandlerStrategy : IActionHandlerStrategy
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly ITaskFacade _taskFacade;
        private readonly IBotService _botService;
        private static List<PendingAction> _pendingActions;

        public ActionType ActionType => ActionType.StopTaskByName;

        public StopTaskHandlerStrategy(ITimeEntryService timeEntryService, ITaskFacade taskFacade, IBotService botService, List<PendingAction> pendingActions)
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
                var allTasks = await _taskFacade.GetTasksByNameAsync(parsed.TaskName);
                var activeTasks = await FilterActiveTasksAsync(messageDto.FromUserId, allTasks);
                if (activeTasks.Count == 0)
                {
                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, $"⚠️ No active task found with given name: '{parsed.TaskName}'.");
                }
                else if (activeTasks.Count == 1)
                {
                    int? percent = parsed.PercentComplete;

                    if (!percent.HasValue && activeTasks[0].PercentComplete == 0)
                    {
                            percent = 50;
                    }
                    await _timeEntryService.StopTaskAsync(messageDto.FromUserId, activeTasks[0], percent);
                    string extra = parsed.PercentComplete.HasValue ? $"{percent}%" : "";
                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, $"⏹️ Stopped task '{activeTasks[0].Title}' {extra}");
                }
                else
                {
                    _pendingActions.Add(new PendingAction(messageDto.FromUserId, activeTasks, null, null, parsed.PercentComplete, null, ActionType.StopTaskByName, null, null));

                    var sb = new StringBuilder();
                    sb.AppendLine($"🔎 Found several tasks with the name '{parsed.TaskName}'. Please specify which one(s) to stop:");
                    for (int i = 0; i < activeTasks.Count; i++)
                        sb.AppendLine($"{i + 1}. {activeTasks[i].Title} (ID: {activeTasks[i].TaskId})");

                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, sb.ToString());
                }
            }
            else
            {
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, $"❓ I didn't understand your message. A task name is required.");
            }
        }
        public async Task<List<TaskModel>> FilterActiveTasksAsync(string userId, List<TaskModel> tasks)
        {
            var activeTasks = new List<TaskModel>();

            foreach (var task in tasks)
            {
                var activeEntry = await _timeEntryService.GetActiveTimeEntriesAsync(userId, task.Id);
                if (activeEntry != null)
                {
                    activeTasks.Add(task);
                }
            }

            return activeTasks;
        }

    }
}