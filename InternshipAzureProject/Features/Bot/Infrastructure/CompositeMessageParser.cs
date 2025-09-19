using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Interfaces;
using Application.Services;

namespace Infrastructure.Services
{
    public class CompositeMessageParser : IMessageParser
    {
        private readonly IMessageParser _regexParser;
        private readonly IMessageParser _aiParser;

        public CompositeMessageParser(RegexMessageParser regexParser, OpenAIMessageParser aiParser)
        {
            _regexParser = regexParser;
            _aiParser = aiParser;
        }

        public async Task<ParsedMessageDto> Parse(string messageThreadId, string message, DateTimeOffset? currentDate)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return new ParsedMessageDto(ActionType.Unknown, null);
            }

            // 1: regex parser
            var result = await _regexParser.Parse(messageThreadId, message, currentDate);

            // 2: ai parser if regex fails or returns Unknown
            if (result.Action == ActionType.Unknown)
            {
                result = await _aiParser.Parse(messageThreadId, message, currentDate);
            }

            // 3: fallback to Unknown if still null
            if (result == null)
            {
                return new ParsedMessageDto(ActionType.Unknown, null);
            }

            return result;
        }
    }
}
