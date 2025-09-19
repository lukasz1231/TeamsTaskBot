using Domain.Entities.Dtos;
using Domain.Entities.Enums;

namespace Domain.Interfaces
{
    public interface IMessageParser
    {
        Task<ParsedMessageDto> Parse(string messageThreadId, string message, DateTimeOffset? currentDate);
    }
}
