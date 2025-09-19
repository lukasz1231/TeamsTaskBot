using Domain.Entities;
using Domain.Entities.Dtos;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class ActionHandler : IActionHandler
    {
        private readonly ILogger<ActionHandler> _logger;
        private readonly IUserGraphService _userGraphService;
        private readonly ActionHandlerFactory _actionHandlerFactory;
        private readonly IBotService _botService;

        public ActionHandler(ILogger<ActionHandler> logger, IUserGraphService userGraphService, ActionHandlerFactory actionHandlerFactory, IBotService botService)
        {
            _logger = logger;
            _userGraphService = userGraphService;
            _actionHandlerFactory = actionHandlerFactory;
            _botService = botService;
        }

        public async Task HandleActionAsync(ParsedMessageDto parsed, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            var userRoles = await _userGraphService.GetUserRolesAsync(messageDto.FromUserId);

            if (!ActionRoleAuthorization.HasPermission(userRoles, parsed.Action))
            {
                _logger.LogWarning("User {UserId} attempted to perform action {Action} without required permissions.", messageDto.FromUserId, parsed.Action);
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, $"❌ **Access denied.** You don't have permission to perform this action: {parsed.Action}.");
                return;
            }

            try
            {
                var handler = _actionHandlerFactory.GetHandler(parsed.Action);
                await handler.HandleAsync(parsed, messageDto, teamId, channelId);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to get handler for action type: {ActionType}", parsed.Action);
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, "⚠️ I don't know how to handle this request. The command is invalid or not yet implemented.");
            }
        }
    }
}