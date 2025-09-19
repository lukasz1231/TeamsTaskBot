using Domain.Entities;
using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Interfaces;
using System.Text;

namespace Infrastructure.Services.ActionHandlers
{
    public class GetTaskReportHandlerStrategy : IActionHandlerStrategy
    {
        private readonly ILogger<ActionHandler> _logger;
        private readonly ITaskFacade _taskFacade;
        private readonly IReportService _reportService;
        private readonly IBotService _botService;
        private static List<PendingAction> _pendingActions;

        public ActionType ActionType => ActionType.GetTaskReport;

        public GetTaskReportHandlerStrategy(ILogger<ActionHandler> logger, ITaskFacade taskFacade, IReportService reportService, IBotService botService, List<PendingAction> pendingActions)
        {
            _logger = logger;
            _taskFacade = taskFacade;
            _reportService = reportService;
            _botService = botService;
            _pendingActions = pendingActions;
        }

        public async Task HandleAsync(ParsedMessageDto parsed, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            try
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
                        var taskReport = await _reportService.GetTaskReportAsync(tasks[0].Id, parsed.StartDate, parsed.EndDate);
                        var taskReportMessage = _reportService.GenerateTaskReportMessage(taskReport);
                        await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, taskReportMessage);
                    }
                    else
                    {
                        _pendingActions.Add(new PendingAction(messageDto.FromUserId, tasks, null, null, null, null, ActionType.GetTaskReport, parsed.StartDate, parsed.EndDate));

                        var sb = new StringBuilder();
                        sb.AppendLine($"🔎 Found several tasks with the name '{parsed.TaskName}'. Please specify which one to generate report for:");
                        for (int i = 0; i < tasks.Count; i++)
                            sb.AppendLine($"{i + 1}. {tasks[i].Title} (ID: {tasks[i].TaskId})");

                        await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, sb.ToString());
                    }
                }
                else
                {
                    // no task name is specified
                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, "No task name was given");
                }
            }
            catch (InvalidOperationException ex)
            {
                string userFriendlyMessage = $"⚠️ **Error:** No time entries were found for the specified task between {parsed.StartDate} and {parsed.EndDate}. Please check the dates and try again.";
                _logger.LogWarning(ex, userFriendlyMessage);
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, userFriendlyMessage);
            }
        }
    }
}