namespace Domain.Interfaces { 
    using Domain.Entities.Dtos;
    using Domain.Entities.Enums;
    using Domain.Entities.Models;

    public interface IActionHandlerStrategy
    {
        ActionType ActionType { get; }
        Task HandleAsync(ParsedMessageDto parsed, TeamsMessageDto messageDto, string teamId, string channelId);
    }
}