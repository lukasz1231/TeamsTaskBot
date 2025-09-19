using Domain.Entities;
using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Interfaces;
using System.Text;
namespace Infrastructure.Services.ActionHandlers
{
    public class ParsedNumbersHandlerStrategy : IActionHandlerStrategy
    {
        private readonly ILogger<ActionHandler> _logger;
        private readonly ITaskFacade _taskFacade;
        private readonly ITimeEntryService _timeEntryService;
        private readonly IReportService _reportService;
        private readonly IBotService _botService;
        private static List<PendingAction> _pendingActions;

        public ActionType ActionType => ActionType.ParsedNumbers;

        public ParsedNumbersHandlerStrategy(
            ILogger<ActionHandler> logger,
            ITaskFacade taskFacade,
            ITimeEntryService timeEntryService,
            IReportService reportService,
            IBotService botService,
            List<PendingAction> pendingActions)
        {
            _logger = logger;
            _taskFacade = taskFacade;
            _timeEntryService = timeEntryService;
            _reportService = reportService;
            _botService = botService;
            _pendingActions = pendingActions;
        }

        public async Task HandleAsync(ParsedMessageDto parsed, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            var pendingAction = _pendingActions.FirstOrDefault(pa => pa.userId == messageDto.FromUserId);

            if (pendingAction != default)
            {
                try
                {
                    var validIndexes = new List<int>();
                    var invalidNumbers = new List<int>();

                    foreach (var number in parsed.ParsedNumbers.Distinct())
                    {
                        if (pendingAction.tasks != null && number >= 1 && number <= pendingAction.tasks.Count)
                        {
                            validIndexes.Add(number);
                        }
                        else if (pendingAction.users != null && number >= 1 && number <= pendingAction.users.Count)
                        {
                            validIndexes.Add(number);
                        }
                        else
                        {
                            invalidNumbers.Add(number);
                        }
                    }

                    if (validIndexes.Any())
                    {
                        switch (pendingAction.actionType)
                        {
                            case ActionType.UpdateTaskByName:
                                await HandleUpdateTask(validIndexes, pendingAction, messageDto, teamId, channelId);
                                break;
                            case ActionType.DeleteTaskByName:
                                await HandleDeleteTask(validIndexes, pendingAction, messageDto, teamId, channelId);
                                break;
                            case ActionType.CommentTaskByName:
                                await HandleCommentTask(validIndexes, pendingAction, messageDto, teamId, channelId);
                                break;
                            case ActionType.StartTaskByName:
                                await HandleStartTask(validIndexes, pendingAction, messageDto, teamId, channelId);
                                break;
                            case ActionType.StopTaskByName:
                                await HandleStopTask(validIndexes, pendingAction, messageDto, teamId, channelId);
                                break;
                            case ActionType.AddTimeToTaskByName:
                                await HandleAddTimeToTask(validIndexes, pendingAction, messageDto, teamId, channelId);
                                break;
                            case ActionType.GetTaskReport:
                                await HandleGetTaskReport(validIndexes, pendingAction, messageDto, teamId, channelId);
                                break;
                            case ActionType.GetUserReport:
                                await HandleGetUserReport(validIndexes, pendingAction, messageDto, teamId, channelId);
                                break;
                        }
                    }
                    else
                    {
                        await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, "❌ None of the provided numbers match any tasks or users.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while handling a pending action for user {UserId}.", messageDto.FromUserId);
                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, $"⚠ Error: {ex.Message}");
                }
                finally
                {
                    _pendingActions.Remove(pendingAction);
                }
            }
            else
            {
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, "⚠️ There are no pending actions.");
            }
        }

        private async Task HandleUpdateTask(List<int> validIndexes, PendingAction pendingAction, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            var tasksToProcess = validIndexes.Select(i => pendingAction.tasks[i - 1]).ToList();
            var targetUserIds = pendingAction.TargetUserIds ?? new List<string> { messageDto.FromUserId };
            targetUserIds = targetUserIds.Select(id => id == "-10" ? messageDto.FromUserId : id).ToList();
            foreach (var task in tasksToProcess)
            {
                if(pendingAction.updatedTask.UsersToAssign == null && pendingAction.TargetUserIds != null)
                    pendingAction.updatedTask.UsersToAssign = targetUserIds;
                await _taskFacade.UpdateTaskByIdAsync(task.Id, pendingAction.updatedTask);
            }

            await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
                $"✏️ Updated {tasksToProcess.Count} task(s).");
        }

        private async Task HandleDeleteTask(List<int> validIndexes, PendingAction pendingAction, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            var tasksToProcess = validIndexes.Select(i => pendingAction.tasks[i - 1]).ToList();

            foreach (var task in tasksToProcess)
            {
                await _taskFacade.DeleteTaskAsync(task);
            }

            await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
                $"🗑️ Deleted {tasksToProcess.Count} task(s).");
        }

        private async Task HandleCommentTask(List<int> validIndexes, PendingAction pendingAction, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            var tasksToProcess = validIndexes.Select(i => pendingAction.tasks[i - 1]).ToList();

            foreach (var task in tasksToProcess)
            {
                await _taskFacade.AddCommentAsync(pendingAction.userId, task, pendingAction.content);
            }

            await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
                $"💬 Added comments to {tasksToProcess.Count} task(s).");
        }

        private async Task HandleStartTask(List<int> validIndexes, PendingAction pendingAction, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            var tasksToProcess = validIndexes.Select(i => pendingAction.tasks[i - 1]).ToList();
            int startedCount = 0;
            List<string> failedTasks = new List<string>();

            foreach (var task in tasksToProcess)
            {
                try
                {
                    await _timeEntryService.StartTaskAsync(pendingAction.userId, task);
                    startedCount++;
                }
                catch (InvalidOperationException ex)
                {
                    failedTasks.Add($"{task.Title} ({ex.Message})");
                }
            }

            // Tworzymy czytelny komunikat
            var sb = new StringBuilder();
            if (startedCount > 0)
                sb.AppendLine($"▶️ Successfully started {startedCount} task(s).");

            if (failedTasks.Count > 0)
            {
                sb.AppendLine($"⚠️ Could not start {failedTasks.Count} task(s):");
                foreach (var fail in failedTasks)
                    sb.AppendLine($"- {fail}");
            }

            await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, sb.ToString());
        }


        private async Task HandleStopTask(List<int> validIndexes, PendingAction pendingAction, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            var tasksToProcess = validIndexes.Select(i => pendingAction.tasks[i - 1]).ToList();

            foreach (var task in tasksToProcess)
            {
                int? percent = pendingAction.percentComplete;

                if (!percent.HasValue && task.PercentComplete == 0)
                    percent = 50;

                await _timeEntryService.StopTaskAsync(pendingAction.userId, task, percent);
                
            }

            await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
                $"⏹️ Stopped {tasksToProcess.Count} task(s).");
        }

        private async Task HandleAddTimeToTask(List<int> validIndexes, PendingAction pendingAction, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            var tasksToProcess = validIndexes.Select(i => pendingAction.tasks[i - 1]).ToList();
            var targetUserIds = pendingAction.TargetUserIds ?? new List<string> { messageDto.FromUserId };
            targetUserIds = targetUserIds.Select(id => id == "-10" ? messageDto.FromUserId : id).ToList();
            foreach (var task in tasksToProcess)
            {
                foreach (var userId in targetUserIds)
                {
                    await _timeEntryService.AddTimeToTaskAsync(userId, task, pendingAction.time ?? 0, pendingAction.percentComplete ?? 0);
                }
            }

            await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId,
                $"⏱️ Logged time to {tasksToProcess.Count} task(s) for {targetUserIds.Count} user(s).");
        }

        private async Task HandleGetTaskReport(List<int> validIndexes, PendingAction pendingAction, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            var tasksToProcess = validIndexes.Select(i => pendingAction.tasks[i - 1]).ToList();

            foreach (var task in tasksToProcess)
            {
                var taskReport = await _reportService.GetTaskReportAsync(task.Id, pendingAction.startDate, pendingAction.endDate);
                var taskReportMessage = _reportService.GenerateTaskReportMessage(taskReport);
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, taskReportMessage);
            }
        }

        private async Task HandleGetUserReport(List<int> validIndexes, PendingAction pendingAction, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            var usersToProcess = validIndexes.Select(i => pendingAction.users[i - 1]).ToList();

            foreach (var user in usersToProcess)
            {
                var userReport = await _reportService.GetUserReportAsync(user.UserId, pendingAction.startDate, pendingAction.endDate);
                var userReportMessage = _reportService.GenerateUserReportMessage(userReport);
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, userReportMessage);
            }
        }
    }
}
