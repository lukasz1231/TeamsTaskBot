using Domain.Interfaces;
using Domain.Entities;
using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using System.Text;


namespace Infrastructure.Services.ActionHandlers
{
    public class CommentTaskHandlerStrategy : IActionHandlerStrategy
    {
        private readonly ILogger<ActionHandler> _logger;
        private readonly ITaskFacade _taskFacade;
        private readonly IBotService _botService;
        private static List<PendingAction> _pendingActions;

        public ActionType ActionType => ActionType.CommentTaskByName;

        public CommentTaskHandlerStrategy(ILogger<ActionHandler> logger, ITaskFacade taskFacade, IBotService botService, List<PendingAction> pendingActions)
        {
            _logger = logger;
            _taskFacade = taskFacade;
            _botService = botService;
            _pendingActions = pendingActions;
        }

        public async Task HandleAsync(ParsedMessageDto parsed, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            if (!string.IsNullOrWhiteSpace(parsed.TaskName) && !string.IsNullOrWhiteSpace(parsed.Content))
            {
                var tasks = await _taskFacade.GetTasksByNameAsync(parsed.TaskName);
                if (tasks.Count == 0)
                {
                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, $"⚠️ No task found with given name: '{parsed.TaskName}'.");
                }
                else if (tasks.Count == 1)
                {
                    await _taskFacade.AddCommentAsync(messageDto.FromUserId, tasks[0], parsed.Content);
                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, $"💬 Added comment to task '{tasks[0].Title}'.");
                }
                else
                {
                    _pendingActions.Add(new PendingAction(messageDto.FromUserId, tasks, null, parsed.Content, null, null, ActionType.CommentTaskByName, null, null));

                    var sb = new StringBuilder();
                    sb.AppendLine($"🔎 Found several tasks with the name '{parsed.TaskName}'. Please specify which one to comment on:");
                    for (int i = 0; i < tasks.Count; i++)
                        sb.AppendLine($"{i + 1}. {tasks[i].Title} (ID: {tasks[i].TaskId})");

                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, sb.ToString());
                }
            }
            else
            {
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, $"❓ I didn't understand your message. Task name or comment is missing.");
            }
        }
    }
}