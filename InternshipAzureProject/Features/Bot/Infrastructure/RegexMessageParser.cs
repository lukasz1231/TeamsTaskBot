using System.Text.RegularExpressions;
using Domain.Entities.Dtos;
using Domain.Entities.Enums;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    

    public class RegexMessageParser : IMessageParser
    {
        public async Task<ParsedMessageDto> Parse(string messageThreadId, string message, DateTimeOffset? currentDate)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return new ParsedMessageDto(ActionType.Unknown, null);
            }

            var parsers = new (Regex Regex, ActionType Action, Func<Match, ParsedMessageDto> Parser)[]
            {
                // Smalltalk: "cześć", "hej", "witam", "dzień dobry", "siema", "hello", "hi", "good morning", "hi there"
                (new Regex(@"^(cześć|hej|witam|dzień dobry|siema|hello|hi|good morning|hi there)$", RegexOptions.IgnoreCase),
                    ActionType.Smalltalk,
                    m => new ParsedMessageDto(ActionType.Smalltalk, null, null, "Hello, how can I help You?")),

                // Smalltalk: "dzięki", "dziękuję", "thanks", "thx", "ok", "okej"
                (new Regex(@"^(dzięki|dziękuję|thanks|thx|ok|okej)$", RegexOptions.IgnoreCase),
                    ActionType.Smalltalk,
                    m => new ParsedMessageDto(ActionType.Smalltalk, null, null, "You're welcome! 😊")),
            };

            foreach (var parser in parsers)
            {
                var match = parser.Regex.Match(message);
                if (match.Success)
                {
                    return parser.Parser(match);
                }
            }

            return new ParsedMessageDto(ActionType.Unknown);
        }
    }
}
