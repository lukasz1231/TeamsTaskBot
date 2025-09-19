using AdaptiveCards;

namespace Domain.Interfaces
{
    public interface IBotService
    {
        public Task SendMessageAsync(string teamId, string channelId, string message);
        public Task ReplyToMessageAsync(string teamId, string channelId, string parentMessageId, string? message = null, AdaptiveCard? adaptiveCard = null);

    }
}
