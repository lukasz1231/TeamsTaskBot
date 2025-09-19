using Domain.Entities.Dtos;

namespace Domain.Interfaces
{
    public interface IActionHandler
    {
        public Task HandleActionAsync(ParsedMessageDto parsed, TeamsMessageDto messageDto, string teamId, string channelId);
    }
}
