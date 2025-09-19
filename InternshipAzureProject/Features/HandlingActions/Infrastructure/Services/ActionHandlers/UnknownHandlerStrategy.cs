using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Interfaces;

namespace Infrastructure.Services.ActionHandlers
{
    public class UnknownActionHandlerStrategy : IActionHandlerStrategy
    {
        private readonly IBotService _botService;

        public ActionType ActionType => ActionType.Unknown;

        public UnknownActionHandlerStrategy(IBotService botService)
        {
            _botService = botService;
        }

        public async Task HandleAsync(ParsedMessageDto parsed, TeamsMessageDto messageDto, string teamId, string channelId)
        {
            await _botService.ReplyToMessageAsync(teamId, channelId, messageDto.ReplyToId, $"❓ I didn't understand your message: {messageDto.Body}");
        }
    }
}