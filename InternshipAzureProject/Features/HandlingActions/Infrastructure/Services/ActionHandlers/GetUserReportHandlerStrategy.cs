using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Interfaces;

namespace Infrastructure.Services.ActionHandlers
{
    public class GetUserReportHandlerStrategy : IActionHandlerStrategy
    {
        private readonly ILogger<ActionHandler> _logger;
        private readonly IReportService _reportService;
        private readonly IBotService _botService;

        public ActionType ActionType => ActionType.GetUserReport;

        public GetUserReportHandlerStrategy(
            ILogger<ActionHandler> logger,
            IReportService reportService,
            IBotService botService)
        {
            _logger = logger;
            _reportService = reportService;
            _botService = botService;
        }

        public async Task HandleAsync(
            ParsedMessageDto parsed,
            TeamsMessageDto messageDto,
            string teamId,
            string channelId)
        {
            try
            {
                // reports for specified users
                if (parsed.userIds != null && parsed.userIds.Any())
                {
                    foreach (var parsedUserId in parsed.userIds)
                    {
                        var userId = parsedUserId;
                        if (userId == "-10")
                            userId = messageDto.FromUserId; 
                        var userReport = await _reportService.GetUserReportAsync(userId, parsed.StartDate, parsed.EndDate);
                        var userReportMessage = _reportService.GenerateUserReportMessage(userReport);

                        await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, userReportMessage);
                    }
                }
                else
                {
                    // Fallback - report for message sender
                    var userReport = await _reportService.GetUserReportAsync(messageDto.FromUserId, parsed.StartDate, parsed.EndDate);
                    var userReportMessage = _reportService.GenerateUserReportMessage(userReport);

                    await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, userReportMessage);
                }
            }
            catch (InvalidOperationException ex)
            {
                string userFriendlyMessage =
                    $"⚠️ **Error:** No time entries were found between {parsed.StartDate} and {parsed.EndDate}. Please check the dates and try again.";
                _logger.LogWarning(ex, userFriendlyMessage);
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, userFriendlyMessage);
            }
        }
    }
}
