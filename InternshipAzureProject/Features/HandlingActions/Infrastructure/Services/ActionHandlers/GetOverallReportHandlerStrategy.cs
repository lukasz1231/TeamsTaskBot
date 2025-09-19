using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Interfaces;

namespace Infrastructure.Services.ActionHandlers
{
    public class GetOverallReportHandlerStrategy : IActionHandlerStrategy
    {
        private readonly ILogger<ActionHandler> _logger;
        private readonly IReportService _reportService;
        private readonly IBotService _botService;

        public ActionType ActionType => ActionType.GetOverallReport;

        public GetOverallReportHandlerStrategy(ILogger<ActionHandler> logger, IReportService reportService, IBotService botService)
        {
            _logger = logger;
            _reportService = reportService;
            _botService = botService;
        }

        public async Task HandleAsync(ParsedMessageDto parsed, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            try
            {
                var overallReport = await _reportService.GetOverallReportAsync(parsed.StartDate, parsed.EndDate);
                var overallReportMessage = _reportService.GenerateOverallReportMessage(overallReport);
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, overallReportMessage);
            }
            catch (InvalidOperationException ex)
            {
                string userFriendlyMessage = $"⚠️ **Error:** No overall time entries were found between {parsed.StartDate} and {parsed.EndDate}. Please check the dates and try again.";
                _logger.LogWarning(ex, userFriendlyMessage);
                await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, userFriendlyMessage);
            }
        }
    }
}